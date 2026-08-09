#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
ATEditor 技能 JSON 类名迁移脚本

批量替换 JSON 文件中旧类名为新类名，以适配代码重构后的反序列化。

替换规则：
  - SkillAnimationClip  → AnimationClip
  - SkillAudioClip      → AudioClip          (预留，当前 JSON 未使用)
  - ComboWindowClip     → RouteWindowClip
  - ComboWindowTrack    → RouteWindowTrack
  - RuntimeComboWindowProcess → RuntimeRouteWindowProcess (预留，当前 JSON 未使用)

用法：
  python migrate_class_names.py                    # 默认处理当前目录下所有 .json
  python migrate_class_names.py --dry-run           # 仅打印会修改的文件，不实际写入
  python migrate_class_names.py --dir D:\path\to    # 指定目标目录
"""

import os
import sys
import argparse
import glob

# ==================== 替换映射表 ====================
REPLACEMENTS = [
    # (旧名称, 新名称) — 注意顺序：长串优先，避免短串误匹配
    ('"SkillAnimationClip"',        '"AnimationClip"'),
    ('"SkillAudioClip"',            '"AudioClip"'),
    ('"ComboWindowClip"',           '"RouteWindowClip"'),
    ('"ComboWindowTrack"',          '"RouteWindowTrack"'),
    ('"RuntimeComboWindowProcess"', '"RuntimeRouteWindowProcess"'),
]


def migrate_file(filepath: str, dry_run: bool = False) -> dict:
    """
    对单个 JSON 文件执行类名替换。
    返回 { 'changed': bool, 'counts': { old_name: hit_count } }
    """
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original = content
    counts = {}

    for old, new in REPLACEMENTS:
        n = content.count(old)
        if n > 0:
            counts[old] = n
            content = content.replace(old, new)

    changed = content != original

    if changed and not dry_run:
        with open(filepath, 'w', encoding='utf-8', newline='') as f:
            f.write(content)

    return {'changed': changed, 'counts': counts}


def main():
    parser = argparse.ArgumentParser(description='ATEditor 技能 JSON 类名迁移工具')
    parser.add_argument('--dir', default=os.path.dirname(os.path.abspath(__file__)),
                        help='JSON 文件所在目录（默认：脚本所在目录）')
    parser.add_argument('--dry-run', action='store_true',
                        help='仅预览变更，不实际修改文件')
    parser.add_argument('--recursive', '-r', action='store_true',
                        help='递归搜索子目录')
    args = parser.parse_args()

    target_dir = args.dir
    if not os.path.isdir(target_dir):
        print(f'[错误] 目录不存在: {target_dir}')
        sys.exit(1)

    # 收集 JSON 文件
    if args.recursive:
        pattern = os.path.join(target_dir, '**', '*.json')
        json_files = glob.glob(pattern, recursive=True)
    else:
        pattern = os.path.join(target_dir, '*.json')
        json_files = glob.glob(pattern)

    if not json_files:
        print(f'[提示] 目录中未找到 .json 文件: {target_dir}')
        sys.exit(0)

    # 统计
    total_files = len(json_files)
    modified_files = 0
    total_replacements = 0
    replacement_summary = {}

    mode_label = '[预览模式]' if args.dry_run else '[执行模式]'
    print(f'\n{mode_label} 扫描目录: {target_dir}')
    print(f'  找到 {total_files} 个 JSON 文件\n')
    print('  替换规则:')
    for old, new in REPLACEMENTS:
        print(f'    {old:40s} -> {new}')
    print()

    for filepath in sorted(json_files):
        result = migrate_file(filepath, dry_run=args.dry_run)
        if result['changed']:
            modified_files += 1
            filename = os.path.relpath(filepath, target_dir)
            detail_parts = []
            for old_name, count in result['counts'].items():
                total_replacements += count
                replacement_summary[old_name] = replacement_summary.get(old_name, 0) + count
                detail_parts.append(f'{old_name} x{count}')
            detail = ', '.join(detail_parts)
            action = '将修改' if args.dry_run else '已修改'
            print(f'  [OK] {action}: {filename}  [{detail}]')

    # 汇总
    print(f'\n{"="*60}')
    print(f'  扫描文件总数:  {total_files}')
    print(f'  修改文件数:    {modified_files}')
    print(f'  替换总次数:    {total_replacements}')
    if replacement_summary:
        print(f'\n  按类名统计:')
        for old_name, count in sorted(replacement_summary.items()):
            new_name = dict(REPLACEMENTS).get(old_name, '?')
            print(f'    {old_name:40s} -> {new_name:30s}  x{count}')
    print(f'{"="*60}\n')

    if args.dry_run and modified_files > 0:
        print('  提示: 以上为预览结果，实际文件未修改。移除 --dry-run 参数以执行替换。\n')


if __name__ == '__main__':
    main()
