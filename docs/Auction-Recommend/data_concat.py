# -*- coding: utf-8 -*-
from __future__ import annotations

import argparse
import json
import os
from typing import Dict, List, Tuple
from tqdm import tqdm
import yaml


def list_court_areas(base_dir: str) -> Dict[str, List[str]]:
    out: Dict[str, List[str]] = {}
    if not os.path.isdir(base_dir):
        return out
    for court in sorted(os.listdir(base_dir)):
        court_path = os.path.join(base_dir, court)
        if not os.path.isdir(court_path):
            continue
        areas = [a for a in sorted(os.listdir(court_path)) if os.path.isdir(os.path.join(court_path, a))]
        if areas:
            out[court] = areas
    return out


def load_json_files(dir_path: str) -> List[Tuple[str, dict]]:
    items: List[Tuple[str, dict]] = []
    if not os.path.isdir(dir_path):
        return items
    for name in sorted(os.listdir(dir_path)):
        if not name.lower().endswith('.json'):
            continue
        p = os.path.join(dir_path, name)
        try:
            with open(p, 'r', encoding='utf-8') as f:
                data = json.load(f)
            items.append((p, data))
        except Exception:
            continue
    return items


def safe_int(x, default=10**9):
    try:
        return int(x)
    except Exception:
        return default


def concat_one_pair(prev_dir: str, new_dir: str, out_dir: str, court: str, area: str, dry_run: bool = False) -> Tuple[int, int, List[dict]]:
    prev_items = load_json_files(os.path.join(prev_dir, court, area))
    new_items = load_json_files(os.path.join(new_dir, court, area))

    # 우선순위: previous → new, case_no 중복은 previous 우선
    def order_key(it: Tuple[str, dict], is_prev: bool) -> Tuple[int, bool]:
        _, data = it
        return (safe_int(data.get('seq')), not is_prev)  # prev 먼저, seq 오름차순

    # previous 먼저 정렬
    prev_items.sort(key=lambda it: order_key(it, True))
    new_items.sort(key=lambda it: order_key(it, False))

    # 중복 제거 없이 previous → new 순으로 모두 사용
    combined: List[dict] = [data for _, data in prev_items] + [data for _, data in new_items]

    # 출력 디렉토리 준비
    target_dir = os.path.join(out_dir, court, area)
    if not dry_run:
        os.makedirs(target_dir, exist_ok=True)
        # 기존 json 제거
        for name in os.listdir(target_dir):
            if name.lower().endswith('.json'):
                try:
                    os.remove(os.path.join(target_dir, name))
                except Exception:
                    pass

    # 재번호(seq) 부여 및 저장
    written = 0
    updated_items: List[dict] = []
    for i, data in enumerate(combined, 1):
        data = dict(data)
        data['seq'] = i
        # court/area 보정
        data['court'] = data.get('court') or court
        data['area'] = data.get('area') or area
        fname = f"{area}_{i}.json"
        if not dry_run:
            try:
                with open(os.path.join(target_dir, fname), 'w', encoding='utf-8') as f:
                    json.dump(data, f, ensure_ascii=False, indent=2)
                written += 1
            except Exception:
                pass
        updated_items.append(data)
    return (len(prev_items), written, updated_items)


def main():
    parser = argparse.ArgumentParser(
        description='두 개의 데이터 디렉토리를 합쳐서 출력 디렉토리에 저장 (공통 court/area만 처리)'
    )
    # 기존 방식: --root 기준으로 data/previous, data/new, data/total 사용
    parser.add_argument('--root', default='data', help='루트 디렉토리 (기본: data, 하위 previous/new/total 사용)')
    # 새 방식: 디렉토리를 직접 지정
    parser.add_argument('--prev_dir', default='', help='기존 데이터 디렉토리 (예: data/2_202501)')
    parser.add_argument('--new_dir', default='', help='추가 데이터 디렉토리 (예: data/3_202509)')
    parser.add_argument('--out_dir', default='', help='결과를 저장할 출력 디렉토리 (예: data/total_merged)')
    parser.add_argument('--dry_run', action='store_true', help='실제 쓰기 없이 요약만 출력')
    parser.add_argument('--use_progress', action='store_true', help='progress YAML을 읽어 이미 concat한 court/area는 스킵')
    parser.add_argument('--progress_path', default='', help='진행상황 YAML 경로(기본: out_dir/merge_progress.yaml)')
    parser.add_argument('--update_progress', action='store_true', help='처리 완료한 court/area를 진행상황 YAML에 기록')
    args = parser.parse_args()

    # 디렉토리 결정 로직
    if args.prev_dir and args.new_dir and args.out_dir:
        # 새 방식: 사용자가 모든 디렉토리를 직접 지정
        prev_dir = args.prev_dir
        new_dir = args.new_dir
        out_dir = args.out_dir
    else:
        # 기존 방식 유지: root 하위에 previous / new / total
        prev_dir = os.path.join(args.root, 'previous')
        new_dir = os.path.join(args.root, 'new')
        out_dir = os.path.join(args.root, 'total')

    default_progress = os.path.join(out_dir, 'merge_progress.yaml')
    progress_path = args.progress_path or default_progress

    # 진행상황 유틸
    def _load_progress(path: str):
        if not args.use_progress:
            return {"completed": []}
        try:
            with open(path, 'r', encoding='utf-8') as f:
                data = yaml.safe_load(f) or {}
        except Exception:
            data = {}
        if "completed" not in data or not isinstance(data.get("completed"), list):
            data["completed"] = []
        return data

    def _save_progress(path: str, data: dict):
        if not args.update_progress:
            return
        os.makedirs(os.path.dirname(os.path.abspath(path)) or ".", exist_ok=True)
        data["last_updated"] = data.get("last_updated") or ""
        try:
            with open(path, 'w', encoding='utf-8') as f:
                yaml.safe_dump(data, f, allow_unicode=True, sort_keys=False)
        except Exception:
            pass

    def _is_completed(court: str, area: str, prog: dict) -> bool:
        comp = prog.get("completed") or []
        pair_str = f"{court} {area}".strip()
        for item in comp:
            # 문자열: 법원 전체 또는 "법원 지역" 완성 문자열
            if isinstance(item, str):
                s = item.strip()
                if s == court or s == pair_str:
                    return True
            # dict: {court: ..., area: ...}
            elif isinstance(item, dict):
                c = str(item.get("court") or "").strip()
                a = str(item.get("area") or "").strip()
                if c and a and c == court and a == area:
                    return True
                if c and not a and c == court:
                    return True
        return False

    def _append_completed(prog: dict, pairs: List[Tuple[str, str]]) -> dict:
        comp = prog.get("completed") or []
        existing = set()
        for it in comp:
            if isinstance(it, str):
                existing.add(it.strip())
            elif isinstance(it, dict):
                cs = f"{str(it.get('court') or '').strip()} {str(it.get('area') or '').strip()}".strip()
                if cs:
                    existing.add(cs)
        for c, a in pairs:
            s = f"{c} {a}".strip()
            if s and s not in existing:
                comp.append(s)
                existing.add(s)
        prog["completed"] = comp
        return prog

    progress = _load_progress(progress_path)

    prev_map = list_court_areas(prev_dir)
    new_map = list_court_areas(new_dir)

    courts = sorted(set(prev_map.keys()) & set(new_map.keys()))
    if not courts:
        print('공통 법원이 없습니다. 종료합니다.')
        return

    # 공통 court/area 페어 목록 준비
    pairs: List[Tuple[str, str]] = []
    for court in courts:
        prev_areas = set(prev_map.get(court, []))
        new_areas = set(new_map.get(court, []))
        for area in sorted(prev_areas & new_areas):
            if args.use_progress and _is_completed(court, area, progress):
                continue
            pairs.append((court, area))

    total_written = 0
    court_to_rows: Dict[str, List[dict]] = {}
    processed_pairs: List[Tuple[str, str]] = []
    for court, area in tqdm(pairs, desc="Merging", unit="pair"):
        prev_count, written, rows = concat_one_pair(prev_dir, new_dir, out_dir, court, area, dry_run=args.dry_run)
        total_written += written
        if not args.dry_run:
            court_to_rows.setdefault(court, []).extend(rows)
        tqdm.write(f"[{court} / {area}] prev={prev_count} → written={written}")
        processed_pairs.append((court, area))

    # 법원별 CSV 생성
    if not args.dry_run and court_to_rows:
        try:
            import pandas as pd
            for court, rows in court_to_rows.items():
                if not rows:
                    continue
                df = pd.DataFrame(rows)
                csv_path = os.path.join(out_dir, f"{court}.csv")
                df.to_csv(csv_path, index=False, encoding='utf-8-sig')
                tqdm.write(f"CSV 저장: {csv_path} (rows={len(df)})")
        except Exception as e:
            print("경고: CSV 저장 중 오류 -", e)

    # 진행상황 업데이트
    if args.update_progress and processed_pairs:
        try:
            progress = _append_completed(progress, processed_pairs)
            _save_progress(progress_path, progress)
        except Exception:
            pass

    print(f"완료: court-area 쌍 {len(pairs)}건, 작성 파일 {total_written}건 (출력: {out_dir})")


if __name__ == '__main__':
    main()


