"""Import attributed portrait and table UI assets, without replacing card art."""
from pathlib import Path
import concurrent.futures, hashlib, json, subprocess

root = Path(__file__).resolve().parents[2]
dst = root / 'assets/sanguosha'
manifest = json.loads((dst / 'sources.json').read_text())
tree = json.loads((root / 'research/sanguosha/qs-tree.json').read_text())
paths = {e['path'] for e in tree['tree']}
requests = {}
for card in (dst / 'generals').glob('*.jpg'):
    name = card.stem
    old = 'image/fullskin/generals/full/nos_' + name + '.png'
    source = old if old in paths else 'image/fullskin/generals/full/' + name + '.png'
    if source in paths:
        requests[source] = 'portraits/' + name + '.png'
for k in ['wei', 'shu', 'wu', 'qun', 'god']:
    requests['image/fullskin/kingdom/frame/dashboard/' + k + '.png'] = 'table/frame-' + k + '.png'
for n in range(6):
    requests['image/fullskin/system/magatamas/' + str(n) + '.png'] = 'table/hp-' + str(n) + '.png'
for name in ['dashboard-avatar', 'dashboard-equip', 'dashboard-hand', 'photo-back']:
    requests['image/fullskin/system/' + name + '.png'] = 'table/' + name + '.png'
for state in ['normal', 'hover', 'down', 'disabled']:
    requests['image/fullskin/system/button/skill/proactive/1-' + state + '.png'] = 'table/skill-' + state + '.png'
for e in paths:
    if e.startswith('image/fullskin/small-equips/') and e.endswith('.png'):
        requests[e] = 'table/equips/' + Path(e).name

def fetch(pair):
    source, rel = pair
    target = dst / rel
    target.parent.mkdir(exist_ok=True, parents=True)
    if not target.exists():
        subprocess.run(['curl', '-fsSL', '--retry', '2', '--max-time', '45',
                        'https://raw.githubusercontent.com/Mogara/QSanguosha-v2/' + tree['sha'] + '/' + source,
                        '-o', str(target)], check=True)
    return {'source': source, 'file': rel, 'sha256': hashlib.sha256(target.read_bytes()).hexdigest()}

with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
    records = list(pool.map(fetch, requests.items()))
merged = {e['file']: e for e in manifest['files']}
merged.update({e['file']: e for e in records})
manifest['files'] = list(merged.values())
(dst / 'sources.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
print('Imported', len(records), 'attributed table assets')
