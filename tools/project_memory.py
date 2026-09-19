#!/usr/bin/env python3
"""Bounded offline Chinese vector retrieval. Search NEVER reads/indexes source documents."""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
import math
from pathlib import Path
import platform
import re
import shutil
import sqlite3
import struct
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]
DB = ROOT / '.project-memory/index.sqlite3'
CONFIG = ROOT / 'tools/project_memory_sources.json'
SCOPES = ('all', 'current', 'navigation', 'evidence')
EXCLUDED = {'qa', 'qa-game', 'dist', 'research', 'archive', 'bin', 'obj', 'lib', '.git', '.claude', 'Claude outputs'}


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def stamp(path):
    s = path.stat()
    return f'{s.st_mtime_ns}:{s.st_ctime_ns}:{s.st_size}:{s.st_ino}'


def sources():
    cfg = json.loads(CONFIG.read_text())
    result = {}
    for name, scope in cfg['sources'].items():
        p = Path(name)
        if p.is_absolute() or '..' in p.parts or scope not in SCOPES[1:]:
            raise ValueError('Invalid source: ' + name)
        path = ROOT / p
        rel = path.resolve().relative_to(ROOT.resolve())
        if EXCLUDED.intersection((*p.parts, *rel.parts)):
            raise ValueError('Excluded source: ' + name)
        if path.exists():
            if not path.is_file() or path.stat().st_size > cfg['max_file_bytes']:
                raise ValueError('Source is not a small document: ' + name)
            result[name] = (scope, stamp(path))
    return result


def features(text):
    words = []
    for word in re.findall(r'[a-z0-9_./-]+|[\u3400-\u9fff]+', text.lower()):
        if re.fullmatch(r'[\u3400-\u9fff]+', word):
            words.extend(word[i:i+n] for n in (2,) for i in range(len(word)-n+1))
            if len(word) == 1:
                words.append(word)
        else:
            words.append(word)
            words.extend(p for p in re.split(r'[_./-]+', word) if len(p) > 1 and p != word)
    return {w: 1 + math.log(n) for w, n in Counter(words).items()}


def chunks(text):
    title, buf, start, size = '', [], 1, 0
    for n, line in enumerate(text.splitlines(), 1):
        if buf and (line.startswith('#') or size + len(line) > 650):
            yield start, n-1, title, '\n'.join(buf).strip()
            buf, size = [], 0
        if line.startswith('#'):
            title = line.lstrip('# ').strip()
        if not buf:
            start = n
        if len(line) > 650:
            for offset in range(0, len(line), 650):
                yield n, n, title, line[offset:offset+650]
        else:
            buf.append(line)
            size += len(line) + 1
    if buf:
        yield start, len(text.splitlines()), title, '\n'.join(buf).strip()


def embed(texts):
    if platform.system() != 'Darwin':
        raise RuntimeError('Chinese semantic embedding requires macOS. Use --mode lexical for indexed keyword search.')
    swift = ROOT / 'tools/project_embedding.swift'
    key = digest(swift.read_bytes() + platform.mac_ver()[0].encode())[:16]
    folder = DB.parent / 'bin'
    binary = folder / ('embedding-' + key)
    if not binary.exists():
        compiler = shutil.which('swiftc')
        if not compiler:
            raise RuntimeError('swiftc missing; install Apple command line tools before indexing.')
        folder.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=folder) as tmp:
            target = Path(tmp) / 'embedding'
            subprocess.run([compiler, '-O', str(swift), '-o', str(target)], check=True, capture_output=True, timeout=60)
            target.replace(binary)
    p = subprocess.run([str(binary)], input=json.dumps({'texts': texts}), text=True,
                       capture_output=True, check=True, timeout=60)
    data = json.loads(p.stdout)
    if len(data['vectors']) != len(texts):
        raise ValueError('Incomplete embeddings')
    vectors = []
    for v in data['vectors']:
        if len(v) != data['dimensions'] or not all(math.isfinite(x) for x in v):
            raise ValueError('Invalid embedding')
        norm = math.sqrt(sum(x*x for x in v))
        if norm == 0:
            raise ValueError('Empty embedding')
        vectors.append(struct.pack('<' + 'f'*len(v), *(x/norm for x in v)))
    return data['model'], data['dimensions'], vectors


def unpack(blob):
    return struct.unpack('<' + 'f'*(len(blob)//4), blob)


def connect():
    DB.parent.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(DB, timeout=10)
    db.row_factory = sqlite3.Row
    db.execute('PRAGMA foreign_keys=ON')
    if db.execute('PRAGMA user_version').fetchone()[0] not in (0, 1):
        raise RuntimeError('Unknown database version; preserve it before migration.')
    if db.execute('PRAGMA user_version').fetchone()[0] == 1:
        return db
    db.executescript('''
      CREATE TABLE IF NOT EXISTS meta(key TEXT PRIMARY KEY, value TEXT NOT NULL);
      CREATE TABLE IF NOT EXISTS docs(path TEXT PRIMARY KEY, scope TEXT, stamp TEXT, digest TEXT);
      CREATE TABLE IF NOT EXISTS chunks(id INTEGER PRIMARY KEY, fingerprint TEXT UNIQUE,
        title TEXT, text TEXT, embedding BLOB NOT NULL, norm REAL DEFAULT 1);
      CREATE TABLE IF NOT EXISTS locations(path TEXT REFERENCES docs(path) ON DELETE CASCADE,
        chunk INTEGER REFERENCES chunks(id) ON DELETE CASCADE, line INTEGER, end_line INTEGER,
        PRIMARY KEY(path,chunk,line));
      CREATE INDEX IF NOT EXISTS location_chunk ON locations(chunk);
      CREATE INDEX IF NOT EXISTS docs_scope ON docs(scope);
      CREATE TABLE IF NOT EXISTS postings(term TEXT, chunk INTEGER REFERENCES chunks(id) ON DELETE CASCADE,
        weight REAL, PRIMARY KEY(term,chunk)) WITHOUT ROWID;
      CREATE INDEX IF NOT EXISTS postings_chunk ON postings(chunk);
      CREATE TABLE IF NOT EXISTS terms(term TEXT PRIMARY KEY, idf REAL);
      CREATE TABLE IF NOT EXISTS cache(key TEXT PRIMARY KEY, payload TEXT, used REAL);
      PRAGMA user_version=1;
    ''')
    db.commit()
    return db


def backend_signature(kind):
    # Bump the pipeline version when token/chunk semantics change.
    code = b'campus-memory-pipeline-2:' + kind.encode()
    if kind == 'semantic':
        code += (ROOT / 'tools/project_embedding.swift').read_bytes() + platform.mac_ver()[0].encode()
    return digest(code)


def index(db, force=False, backend='auto'):
    started = time.perf_counter()
    if backend not in ('auto', 'semantic', 'lexical'):
        raise ValueError('Invalid indexing backend')
    kind = ('semantic' if platform.system() == 'Darwin' else 'lexical') if backend == 'auto' else backend
    config_bytes = CONFIG.read_bytes()
    present = sources()
    previous = {r['path']: dict(r) for r in db.execute('SELECT * FROM docs')}
    meta = dict(db.execute('SELECT * FROM meta'))
    signature = backend_signature(kind)
    rebuild = force or meta.get('backend') != signature
    known = set() if rebuild else {r[0] for r in db.execute('SELECT fingerprint FROM chunks')}
    changed, touched, checked, pending = [], [], 0, {}
    for path, (scope, state) in present.items():
        old = previous.get(path, {})
        if not rebuild and old.get('stamp') == state and old.get('scope') == scope:
            continue
        raw = (ROOT / path).read_bytes()
        checked += 1
        if stamp(ROOT / path) != state:
            raise RuntimeError('Source changed while indexing; retry: ' + path)
        sha = digest(raw)
        if not rebuild and old.get('digest') == sha and old.get('scope') == scope:
            touched.append((state, path))
            continue
        parts = []
        for line, end, title, text in chunks(raw.decode('utf-8-sig')):
            if not any(t.strip() and not t.startswith('#') for t in text.splitlines()):
                continue
            # Heading context affects meaning; whitespace/code indentation is retained.
            fp = digest((title + '\0' + text).encode())
            parts.append((fp, line, end, title, text))
            if fp not in known:
                pending[fp] = re.sub(r'(?:[A-Za-z0-9_.-]+/)+[^\s；。`]+', '', title + '\n' + text)
        changed.append((path, scope, state, sha, parts))
    removed = set(previous) - set(present)
    if not changed and not removed and not touched and not rebuild:
        return {'changed_files': 0, 'source_files_read': checked, 'embedded_new_chunks': 0,
                'index_ms': round((time.perf_counter()-started)*1000, 2)}
    vectors = {fp: b'' for fp in pending} if kind == 'lexical' else {}
    model, dims = ('lexical-only', 0) if kind == 'lexical' else (meta.get('model'), int(meta.get('dimensions', 0)))
    if rebuild and kind == 'semantic':
        model, dims = None, 0
    items = list(pending.items()) if kind == 'semantic' else []
    for offset in range(0, len(items), 64):
        batch = items[offset:offset+64]
        m, d, result = embed([t for _, t in batch])
        if model and (m != model or d != dims):
            raise RuntimeError('Embedding model changed; rerun index --force.')
        model, dims = m, d
        vectors.update(zip([fp for fp, _ in batch], result))
    # Expensive embedding runs outside the transaction. Recheck both inputs and
    # database generation under the write lock so concurrent work cannot be lost.
    with db:
        db.execute('BEGIN IMMEDIATE')
        now = dict(db.execute('SELECT * FROM meta'))
        if now.get('indexed_at') != meta.get('indexed_at'):
            raise RuntimeError('Index changed concurrently; retry.')
        if CONFIG.read_bytes() != config_bytes or sources() != present:
            raise RuntimeError('Sources changed while indexing; retry.')
        for path in removed:
            db.execute('DELETE FROM docs WHERE path=?', (path,))
        db.executemany('UPDATE docs SET stamp=? WHERE path=?', touched)
        for path, scope, state, sha, parts in changed:
            db.execute('DELETE FROM docs WHERE path=?', (path,))
            db.execute('INSERT INTO docs VALUES(?,?,?,?)', (path, scope, state, sha))
            for fp, line, end, title, text in parts:
                row = db.execute('SELECT id FROM chunks WHERE fingerprint=?', (fp,)).fetchone()
                if row:
                    cid = row[0]
                    if fp in vectors:
                        db.execute('UPDATE chunks SET embedding=? WHERE id=?', (vectors[fp], cid))
                    if rebuild:
                        db.execute('DELETE FROM postings WHERE chunk=?', (cid,))
                else:
                    cid = db.execute('INSERT INTO chunks(fingerprint,title,text,embedding) VALUES(?,?,?,?)',
                                     (fp, title, text, vectors[fp])).lastrowid
                if not row or rebuild:
                    db.executemany('INSERT INTO postings VALUES(?,?,?)',
                                   [(term, cid, weight) for term, weight in features(title+'\n'+text).items()])
                db.execute('INSERT INTO locations VALUES(?,?,?,?)', (path, cid, line, end))
        if changed or removed or rebuild:
            db.execute('DELETE FROM chunks WHERE NOT EXISTS(SELECT 1 FROM locations WHERE chunk=chunks.id)')
            db.execute('DELETE FROM terms')
            count = db.execute('SELECT count(*) FROM chunks').fetchone()[0]
            counts = db.execute('SELECT term,count(*) FROM postings GROUP BY term').fetchall()
            db.executemany('INSERT INTO terms VALUES(?,?)', [(t, math.log((count+1)/(n+1))+1) for t,n in counts])
            norms = db.execute('SELECT chunk,sum(weight*weight*idf*idf) FROM postings JOIN terms USING(term) GROUP BY chunk').fetchall()
            db.executemany('UPDATE chunks SET norm=? WHERE id=?', [(math.sqrt(n), cid) for cid,n in norms])
            for k,v in {'backend': signature, 'backend_kind': kind, 'model': model or 'empty',
                        'dimensions': dims, 'indexed_at': time.time_ns()}.items():
                db.execute('INSERT OR REPLACE INTO meta VALUES(?,?)', (k, str(v)))
            db.execute('DELETE FROM cache')
    return {'changed_files': len(changed)+len(removed), 'metadata_only_files': len(touched),
            'source_files_read': checked, 'embedded_new_chunks': len(pending) if kind == 'semantic' else 0,
            'index_ms': round((time.perf_counter()-started)*1000, 2)}


def search(db, query, limit=3, scope='all', mode='auto'):
    started = time.perf_counter()
    if not query.strip() or len(query) > 500:
        raise ValueError('Use a specific query of 1–500 characters.')
    if scope not in SCOPES or mode not in ('auto', 'hybrid', 'semantic', 'lexical'):
        raise ValueError('Invalid scope or mode')
    limit = max(1, min(limit, 5))
    meta = dict(db.execute('SELECT * FROM meta'))
    if 'indexed_at' not in meta:
        raise RuntimeError('Index missing. Run: python3 -B tools/project_memory.py index')
    if mode == 'auto':
        mode = 'hybrid' if int(meta.get('dimensions', 0)) else 'lexical'
    if mode != 'lexical' and not int(meta.get('dimensions', 0)):
        raise RuntimeError('This index is lexical-only; use --mode lexical or index --backend semantic.')
    key = digest(json.dumps([meta['indexed_at'], query.strip(), limit, scope, mode], ensure_ascii=False).encode())
    cached = db.execute('SELECT payload FROM cache WHERE key=?', (key,)).fetchone()
    if cached:
        payload = json.loads(cached[0])
        payload.update(cache_hit=True, search_ms=round((time.perf_counter()-started)*1000, 2))
        return payload
    # Only vector/norm columns enter ranking. Source text is fetched for final top-k only.
    eligible_sql = 'SELECT DISTINCT l.chunk FROM locations l JOIN docs d ON d.path=l.path'
    args = ()
    if scope != 'all':
        eligible_sql += ' WHERE d.scope=?'
        args = (scope,)
    eligible = {r[0] for r in db.execute(eligible_sql, args)}
    norms = {r[0]:r[1] for r in db.execute('SELECT id,norm FROM chunks')}
    dots, qnorm = defaultdict(float), 0
    query_terms = features(query)
    if query_terms:
        marks = ','.join('?' for _ in query_terms)
        idfs = dict(db.execute('SELECT term,idf FROM terms WHERE term IN ('+marks+')', tuple(query_terms)))
        qnorm = sum((w*idfs.get(t, 1))**2 for t,w in query_terms.items())
        for term, cid, weight in db.execute('SELECT term,chunk,weight FROM postings WHERE term IN ('+marks+')', tuple(query_terms)):
            if cid in eligible:
                dots[cid] += weight*query_terms[term]*idfs.get(term, 1)**2
    lex = {cid:v/((math.sqrt(qnorm) or 1)*(norms[cid] or 1)) for cid,v in dots.items()}
    sem = {}
    if mode != 'lexical' and eligible:
        model, dims, vectors = embed([query])
        generation = db.execute("SELECT value FROM meta WHERE key='indexed_at'").fetchone()
        if not generation or generation[0] != meta['indexed_at']:
            raise RuntimeError('Index changed during search; retry.')
        if model != meta['model'] or dims != int(meta['dimensions']):
            raise RuntimeError('Embedding model changed; run index --force.')
        q = unpack(vectors[0])
        db.create_function('cosine', 1, lambda b: sum(a*v for a,v in zip(q,unpack(b))))
        sem = dict(db.execute('SELECT id,cosine(embedding) AS score FROM chunks WHERE id IN (' +
                              eligible_sql + ') ORDER BY score DESC LIMIT 24', args))
    candidates = set(sem) | set(sorted(lex, key=lex.get, reverse=True)[:24])
    # Exact code/ID matches outside semantic top-24 still need their real vector
    # score; otherwise larger indexes silently under-rank lexical candidates.
    missing = candidates-set(sem)
    if mode == 'hybrid' and missing:
        marks = ','.join('?' for _ in missing)
        sem.update(db.execute('SELECT id,cosine(embedding) FROM chunks WHERE id IN ('+marks+')',tuple(missing)))
    peak = max(lex.values(), default=0) or 1
    ranked = []
    for cid in candidates:
        s, l = sem.get(cid, 0), lex.get(cid, 0)
        if mode == 'semantic' and cid not in sem:
            continue
        if mode == 'lexical' and l <= 0:
            continue
        if mode != 'lexical' and s < .1 and l == 0:
            continue
        score = s if mode == 'semantic' else l if mode == 'lexical' else .65*max(0,s)+.35*l/peak
        ranked.append((score, cid, s, l))
    results, perpath = [], Counter()
    for score, cid, semantic, lexical in sorted(ranked, key=lambda v:(-v[0],v[1])):
        locsql = 'SELECT l.*,d.scope FROM locations l JOIN docs d ON d.path=l.path WHERE l.chunk=?'
        locargs = [cid]
        if scope != 'all':
            locsql += ' AND d.scope=?'
            locargs.append(scope)
        location = db.execute(locsql + ' ORDER BY l.path,l.line LIMIT 1', locargs).fetchone()
        if location is None:
            raise RuntimeError('Index changed during search; retry.')
        if perpath[location['path']] >= 2:
            continue
        row = db.execute('SELECT title,text FROM chunks WHERE id=?', (cid,)).fetchone()
        if row is None:
            raise RuntimeError('Index changed during search; retry.')
        results.append({'path':location['path'], 'line':location['line'], 'end_line':location['end_line'],
                        'title':row['title'], 'text':row['text'][:550], 'truncated':len(row['text'])>550,
                        'scope':location['scope'], 'score':round(score,4),
                        'semantic_score':round(semantic,4), 'lexical_score':round(lexical,4)})
        perpath[location['path']] += 1
        if len(results) == limit:
            break
    payload = {'results':results, 'indexed_at':meta['indexed_at'], 'mode':mode,
               'needs_refinement':not results or (mode != 'lexical' and not lex),
               'source_files_read':0, 'cache_hit':False,
               'search_ms':round((time.perf_counter()-started)*1000, 2)}
    with db:
        db.execute('BEGIN IMMEDIATE')
        current = db.execute("SELECT value FROM meta WHERE key='indexed_at'").fetchone()
        if not current or current[0] != meta['indexed_at']:
            raise RuntimeError('Index changed during search; retry.')
        db.execute('INSERT OR REPLACE INTO cache VALUES(?,?,?)', (key,json.dumps(payload,ensure_ascii=False),time.time()))
        db.execute('DELETE FROM cache WHERE key IN (SELECT key FROM cache ORDER BY used DESC LIMIT -1 OFFSET 64)')
    return payload


def status(db, check=False):
    result = {**dict(db.execute('SELECT * FROM meta')), 'database':str(DB),
              'files':db.execute('SELECT count(*) FROM docs').fetchone()[0],
              'chunks':db.execute('SELECT count(*) FROM chunks').fetchone()[0],
              'locations':db.execute('SELECT count(*) FROM locations').fetchone()[0],
              'text_characters':db.execute('SELECT coalesce(sum(length(text)),0) FROM chunks').fetchone()[0],
              'database_bytes':DB.stat().st_size, 'cached_queries':db.execute('SELECT count(*) FROM cache').fetchone()[0]}
    if check:
        current = sources()
        previous = {r['path']:(r['scope'],r['stamp']) for r in db.execute('SELECT * FROM docs')}
        result['stale_paths'] = sorted(p for p in set(current)|set(previous) if current.get(p) != previous.get(p))
        backend = backend_signature(result.get('backend_kind', 'semantic'))
        result['model_code_changed'] = result.get('backend') != backend
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('command', choices=('search', 'index', 'status', 'compact'))
    p.add_argument('query', nargs='?', default='')
    p.add_argument('--limit', type=int, default=3)
    p.add_argument('--scope', choices=SCOPES, default='all')
    p.add_argument('--mode', choices=('auto', 'hybrid', 'semantic', 'lexical'), default='auto')
    p.add_argument('--backend', choices=('auto', 'semantic', 'lexical'), default='auto')
    p.add_argument('--check', action='store_true')
    p.add_argument('--force', action='store_true')
    a = p.parse_args()
    try:
        with connect() as db:
            if a.command == 'search':
                result = search(db, a.query, a.limit, a.scope, a.mode)
            elif a.command == 'index':
                changes = index(db, a.force, a.backend)
                result = {**status(db), **changes}
            elif a.command == 'compact':
                # Maintenance never silently expands or reindexes the corpus.
                db.execute('DELETE FROM cache')
                db.commit()
                db.execute('VACUUM')
                db.execute('PRAGMA optimize')
                result = status(db)
            else:
                result = status(db, a.check)
        print(json.dumps(result, ensure_ascii=False, indent=2))
    except (ValueError, RuntimeError, OSError, sqlite3.Error, subprocess.SubprocessError) as e:
        p.exit(1, str(e)+'\n')


if __name__ == '__main__':
    main()
