"""发布契约检查：只在临时目录组装，不需要游戏 DLL，也不安装到游戏。"""
from pathlib import Path
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
import assemble_mod
import check_layout
from prepare_minigames_release import build_manual_tree


class PackagingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory()
        cls.addClassCleanup(cls.temp.cleanup)
        cls.root = Path(cls.temp.name)
        cls.dll = cls.root / 'CampusMinigames.dll'
        # Opaque bytes test transport/layout only. This is not a runtime build test.
        cls.dll.write_bytes(b'package-layout-fixture')
        cls.mod = assemble_mod.assemble(cls.dll, cls.root / 'mod', ROOT / 'distribution/workshop/preview.png')

    def test_steam_contract_and_payloads(self):
        check_layout.check_mod(self.mod)
        self.assertEqual((self.mod / check_layout.DLL).read_bytes(), self.dll.read_bytes())
        self.assertEqual(check_layout.inventory(self.mod / 'Cfgs'), check_layout.inventory(ROOT / 'mod/Cfgs'))
        content = self.mod / 'EC2BUnofficialPatch/Minigame'
        for source, target in assemble_mod.ASSET_DIRS.items():
            self.assertEqual(check_layout.inventory(content / target), check_layout.inventory(ROOT / source), source)
        self.assertEqual(check_layout.inventory(content / 'Audio'), check_layout.inventory(ROOT / 'assets/gomoku/audio'))

    def test_manual_package_uses_one_plugin_folder(self):
        plugin = build_manual_tree(self.mod, self.root / 'manual')
        self.assertEqual([p.name for p in plugin.parent.iterdir()], ['CampusMinigames'])
        self.assertEqual([p.name for p in plugin.rglob('*.dll')], ['CampusMinigames.dll'])
        self.assertEqual(check_layout.inventory(plugin / 'Cfgs'), check_layout.inventory(self.mod / 'Cfgs'))
        import json
        registry = json.loads((plugin / 'CustomMinigamecfg.json').read_text())
        self.assertEqual({r['dll'] for r in registry['minigames']}, {'CampusMinigames.dll'})
        self.assertEqual(len(registry['minigames']), 12)

    def test_stale_manifest_is_rejected(self):
        path = self.mod / 'plugins/Unexpected.dll'
        try:
            path.write_bytes(b'unexpected')
            with self.assertRaises(ValueError):
                check_layout.check_mod(self.mod)
        finally:
            path.unlink()


if __name__ == '__main__':
    unittest.main()
