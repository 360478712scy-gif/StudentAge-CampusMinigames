#!/usr/bin/env python3
"""Package a compiled staging for the official 1.94 Mod/plugins loader; never publish."""
import argparse, hashlib, json, shutil, zipfile
from pathlib import Path
p=argparse.ArgumentParser()
p.add_argument('--staging',type=Path,required=True)
p.add_argument('--out',type=Path,required=True)
p.add_argument('--preview',type=Path,help='Existing preview artwork; only format conversion is applied')
p.add_argument('--up',type=Path,help='Optional compatible UP DLL for a local all-in-one candidate')
p.add_argument('--up-source',type=Path,help='Matching UP source checkout, bundled with its license')
a=p.parse_args();root=Path(__file__).resolve().parents[1];out=a.out.resolve()
if out.exists():raise SystemExit('Output exists; use a fresh folder')
out.mkdir(parents=True);plugins=out/'plugins'
shutil.copytree(a.staging/'BepInEx/plugins/StudentAgeCampusMinigames',plugins)
shutil.copy2(a.staging/'BepInEx/plugins/CampusUno/CampusUno.dll',plugins/'CampusUno.dll')
if a.up:shutil.copy2(a.up,plugins/'EC2BUnofficialPatch.dll')
# Reuse existing artwork; this does not create or alter a game asset.
from PIL import Image
Image.open(a.preview or root/'assets/nds/Badges/badge-919999.png').convert('RGB').save(out/'preview.jpg',quality=90)
shutil.copy2(root/'integration/INSTALL.md',out/'安装与角色绑定.md')
for name in ['小游戏配置说明.md','三国杀说明.md','角色武将分配.md','人物对话配置说明.md','第一关胜利剧情说明.md','THIRD_PARTY_NOTICES.md']:
 shutil.copy2(a.staging/name,out/name)
(out/'使用说明.txt').write_text('适用《学生时代》1.94 测试分支。订阅后在游戏内启用并重启。\n工坊版由 Steam 更新；数值配置仍在游戏 BepInEx/config 中。\n'+('本整合版包含兼容 UP；不要同时安装另一份同名 UP 或小游戏。公开发布前须配齐 UP 对应源码和许可。\n' if a.up else '需要同时订阅并启用兼容 1.94 的 UP 前置。\n')+'F7：主界面三国杀测试目录。F8：主界面 NDS。\n三国杀 2006 年年初起出售；NDS 2005 年夏起出售，已有物品不重复出售。\n',encoding='utf-8')
if a.up_source:
 if not a.up: raise SystemExit('--up-source requires --up')
 (out/'Source').mkdir(exist_ok=True)
 with zipfile.ZipFile(out/'Source/UP-source.zip','w',zipfile.ZIP_DEFLATED) as z:
  for f in (a.up_source/'EC2BUnofficialPatch').rglob('*'):
   if f.is_file() and not any(part in {'bin','obj','.git'} for part in f.relative_to(a.up_source).parts):z.write(f,str(f.relative_to(a.up_source)))
  for name in ['EC2BUnofficialPatch.csproj','build.py']:z.write(a.up_source/name,name)
 shutil.copy2(a.up_source/'EC2BUnofficialPatch/LICENSE',out/'UP-LICENSE.txt')
 (out/'UP来源.txt').write_text('原作者 lfw / lfwing、雾雁。对应源码见 Source/UP-source.zip。\n本地兼容修改：按官方启动时的启用目录发现内容；工坊位置由 Steam 管理更新。插件及程序集身份不变。\n')
 instructions=out/'使用说明.txt';instructions.write_text(instructions.read_text().replace('公开发布前须配齐 UP 对应源码和许可。','对应 UP 源码及许可随包提供。'))
(out/'workshop-package.json').write_text(json.dumps({'target':'StudentAge 1.94','distribution':'workshop','includesUP':bool(a.up),'files':{str(f.relative_to(out)):hashlib.sha256(f.read_bytes()).hexdigest() for f in out.rglob('*') if f.is_file()}},ensure_ascii=False,indent=2))
print(out)
