"""Storage and retrieval contracts; fake embeddings only in isolated unit tests."""
import importlib.util
import json
import os
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location('project_memory', Path(__file__).resolve().parents[2] / 'tools/project_memory.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)


class RetrievalContract(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        (self.root/'tools').mkdir()
        (self.root/'tools/project_embedding.swift').write_text('test fixture')
        self.config = self.root/'tools/project_memory_sources.json'
        self.config.write_text(json.dumps({'max_file_bytes':32000, 'sources':{'a.md':'current','b.md':'evidence'}}))
        (self.root/'a.md').write_text('## 阴影\n扑克牌之间的阴影必须贴合圆角。\n')
        (self.root/'b.md').write_text('## 结算\n结算只发放一次奖励。\n')
        self.patches = [patch.object(m,'ROOT',self.root),patch.object(m,'DB',self.root/'index.sqlite3'),
                        patch.object(m,'CONFIG',self.config),patch.object(m.platform,'system',return_value='Darwin'),patch.object(m,'embed',self.embedding)]
        for p in self.patches:
            p.start()
        self.db = m.connect()

    @staticmethod
    def embedding(texts):
        return 'test-only', 4, [struct.pack('<ffff',1,0,0,0) for _ in texts]

    def tearDown(self):
        self.db.close()
        for p in reversed(self.patches):
            p.stop()
        self.tmp.cleanup()

    def test_unchanged_index_reads_nothing_and_update_only_one_file(self):
        m.index(self.db)
        with patch.object(m,'embed',side_effect=AssertionError('unexpected embedding')):
            r = m.index(self.db)
            self.assertEqual((r['source_files_read'],r['changed_files']), (0,0))
        (self.root/'a.md').write_text('## 阴影\n扑克牌之间的阴影已经调整。\n')
        self.assertEqual(m.index(self.db)['source_files_read'],1)

    def test_search_does_not_read_sources_or_reindex(self):
        m.index(self.db)
        with patch.object(m,'sources',side_effect=AssertionError('source enumeration')), \
             patch.object(m,'index',side_effect=AssertionError('reindex')), \
             patch.object(Path,'read_text',side_effect=AssertionError('text read')), \
             patch.object(Path,'read_bytes',side_effect=AssertionError('source read')):
            r = m.search(self.db,'扑克牌阴影',mode='lexical')
            self.assertEqual(r['source_files_read'],0)
            self.assertEqual(r['results'][0]['title'],'阴影')

    def test_duplicate_chunks_survive_removing_one_source(self):
        (self.root/'b.md').write_bytes((self.root/'a.md').read_bytes())
        m.index(self.db)
        self.assertEqual(m.status(self.db)['chunks'],1)
        self.assertEqual(m.status(self.db)['locations'],2)
        (self.root/'a.md').unlink()
        m.index(self.db)
        r = m.search(self.db,'阴影',scope='evidence',mode='lexical')
        self.assertEqual(len(r['results']),1)
        self.assertTrue(r['results'][0]['path'].endswith('b.md'))

    def test_cache_invalidates_after_changes_and_reports_staleness(self):
        m.index(self.db)
        m.search(self.db,'阴影',mode='lexical')
        self.assertTrue(m.search(self.db,'阴影',mode='lexical')['cache_hit'])
        (self.root/'a.md').write_text('## 阴影\n新的圆角阴影规则。\n')
        self.assertEqual(m.status(self.db,check=True)['stale_paths'],['a.md'])
        m.index(self.db)
        r = m.search(self.db,'阴影',mode='lexical')
        self.assertFalse(r['cache_hit'])
        self.assertIn('新的',r['results'][0]['text'])

    def test_embedding_failure_preserves_previous_database(self):
        m.index(self.db)
        before = m.status(self.db)
        (self.root/'a.md').write_text('## 新知识\n尚未建立的词条。\n')
        with patch.object(m,'embed',side_effect=RuntimeError('offline model failed')):
            with self.assertRaises(RuntimeError):
                m.index(self.db)
        self.assertEqual(m.status(self.db)['indexed_at'],before['indexed_at'])
        self.assertIn('圆角',m.search(self.db,'阴影',mode='lexical')['results'][0]['text'])

    def test_scope_limits_and_noise_exclusion(self):
        m.index(self.db)
        self.assertEqual(m.search(self.db,'阴影',scope='evidence',mode='lexical')['results'],[])
        r = m.search(self.db,'阴影',limit=999)
        self.assertLessEqual(len(r['results']),5)
        self.assertTrue(all(len(x['text'])<=550 for x in r['results']))
        cfg = json.loads(self.config.read_text())
        cfg['sources']['qa/log.md']='current'
        self.config.write_text(json.dumps(cfg))
        with self.assertRaisesRegex(ValueError,'Excluded'):
            m.index(self.db)

    def test_empty_index_and_blank_query_are_actionable(self):
        with self.assertRaisesRegex(RuntimeError,'Index missing'):
            m.search(self.db,'阴影')
        with self.assertRaises(ValueError):
            m.search(self.db,' ')

    def test_portable_lexical_backend_never_calls_apple_model(self):
        with patch.object(m, 'embed', side_effect=AssertionError('Apple embedding called')):
            m.index(self.db, backend='lexical')
            result = m.search(self.db, '扑克牌阴影')
            self.assertEqual(result['mode'], 'lexical')
            self.assertEqual(result['results'][0]['title'], '阴影')
            with self.assertRaisesRegex(RuntimeError, 'lexical-only'):
                m.search(self.db, '阴影', mode='semantic')

    def test_touch_without_content_change_preserves_cache_and_vectors(self):
        m.index(self.db)
        m.search(self.db, '阴影', mode='lexical')
        p = self.root / 'a.md'
        state = p.stat()
        os.utime(p, ns=(state.st_atime_ns, state.st_mtime_ns + 1000000))
        with patch.object(m, 'embed', side_effect=AssertionError('unneeded embedding')):
            result = m.index(self.db)
        self.assertEqual(result['metadata_only_files'], 1)
        self.assertEqual(result['changed_files'], 0)
        self.assertTrue(m.search(self.db, '阴影', mode='lexical')['cache_hit'])

    def test_model_upgrade_force_rebuild_is_allowed(self):
        m.index(self.db)
        def upgraded(texts):
            return 'upgraded', 2, [struct.pack('<ff', 1, 0) for _ in texts]
        with patch.object(m, 'embed', side_effect=upgraded):
            m.index(self.db, force=True)
            self.assertEqual(m.status(self.db)['model'], 'upgraded')
            self.assertEqual(len(m.search(self.db, '阴影', mode='semantic')['results']), 2)

    def test_source_change_during_embedding_preserves_previous_index(self):
        m.index(self.db)
        before = m.status(self.db)['indexed_at']
        (self.root / 'a.md').write_text('## 阴影\n第一次修改。\n')
        def concurrent_edit(texts):
            (self.root / 'a.md').write_text('## 阴影\n第二次修改。\n')
            return self.embedding(texts)
        with patch.object(m, 'embed', side_effect=concurrent_edit):
            with self.assertRaisesRegex(RuntimeError, 'Sources changed'):
                m.index(self.db)
        self.assertEqual(m.status(self.db)['indexed_at'], before)
        self.assertIn('圆角', m.search(self.db, '阴影', mode='lexical')['results'][0]['text'])

    def test_concurrent_index_is_not_overwritten(self):
        m.index(self.db)
        (self.root / 'a.md').write_text('## 阴影\n修改之后的阴影。\n')
        def concurrent_index(texts):
            other = m.connect()
            try:
                with patch.object(m, 'embed', side_effect=self.embedding):
                    m.index(other)
            finally:
                other.close()
            return self.embedding(texts)
        with patch.object(m, 'embed', side_effect=concurrent_index):
            with self.assertRaisesRegex(RuntimeError, 'concurrently'):
                m.index(self.db)
        self.assertEqual(m.status(self.db, check=True)['stale_paths'], [])

    def test_query_cache_cannot_write_across_index_generations(self):
        m.index(self.db)
        def reindex_during_search(texts):
            (self.root / 'a.md').write_text('## 阴影\n新版本的阴影。\n')
            other = m.connect()
            try:
                with patch.object(m, 'embed', side_effect=self.embedding):
                    m.index(other)
            finally:
                other.close()
            return self.embedding(texts)
        with patch.object(m, 'embed', side_effect=reindex_during_search):
            with self.assertRaisesRegex(RuntimeError, 'Index changed during search'):
                m.search(self.db, '阴影')
        self.assertEqual(m.status(self.db)['cached_queries'], 0)
        self.assertIn('新版本', m.search(self.db, '阴影', mode='lexical')['results'][0]['text'])

    def test_empty_index_can_be_built_and_searched(self):
        (self.root / 'a.md').unlink()
        (self.root / 'b.md').unlink()
        m.index(self.db, backend='lexical')
        self.assertEqual(m.search(self.db, '不存在')['results'], [])

    def test_cache_is_bounded_and_stores_relative_provenance(self):
        m.index(self.db, backend='lexical')
        for n in range(70):
            result = m.search(self.db, f'阴影 {n}')
            self.assertFalse(Path(result['results'][0]['path']).is_absolute())
        self.assertEqual(m.status(self.db)['cached_queries'], 64)
        self.assertEqual(self.db.execute('PRAGMA integrity_check').fetchone()[0], 'ok')
        self.assertEqual(self.db.execute('PRAGMA foreign_key_check').fetchall(), [])

    def test_symlink_outside_whitelist_root_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            outside = Path(tmp) / 'private.md'
            outside.write_text('not project knowledge')
            (self.root / 'a.md').unlink()
            (self.root / 'a.md').symlink_to(outside)
            with self.assertRaises(ValueError):
                m.index(self.db)

    def test_heading_context_is_part_of_chunk_identity(self):
        # Long sections can emit the same body without repeating their heading.
        def same_body(text):
            yield 2, 2, '阴影' if '阴影' in text else '结算', '相同的说明正文'
        with patch.object(m, 'chunks', side_effect=same_body):
            m.index(self.db, backend='lexical')
        self.assertEqual(m.status(self.db)['chunks'], 2)
        self.assertEqual(m.search(self.db, '结算')['results'][0]['title'], '结算')

    def test_backend_switch_replaces_vectors_without_duplicate_postings(self):
        m.index(self.db, backend='lexical')
        count = self.db.execute('SELECT count(*) FROM postings').fetchone()[0]
        m.index(self.db, backend='semantic')
        self.assertEqual(self.db.execute('SELECT count(*) FROM postings').fetchone()[0], count)
        self.assertEqual(m.search(self.db, '阴影')['mode'], 'hybrid')
        m.index(self.db, backend='lexical')
        self.assertEqual(m.search(self.db, '阴影')['mode'], 'lexical')
        self.assertEqual(self.db.execute('SELECT max(length(embedding)) FROM chunks').fetchone()[0], 0)


if __name__ == '__main__':
    unittest.main()
