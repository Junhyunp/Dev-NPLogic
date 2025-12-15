# -*- coding: utf-8 -*-
from __future__ import annotations

import argparse
import os
import re
from typing import List

import pandas as pd


def parse_rank_from_filename(filename: str) -> int | None:
    base = os.path.basename(filename)
    m = re.search(r"(\d+)\s*순위\.csv$", base)
    if m:
        try:
            return int(m.group(1))
        except Exception:
            return None
    m2 = re.search(r"(\d+)", base)
    if m2:
        try:
            return int(m2.group(1))
        except Exception:
            return None
    return None


def combine_folder(results_dir: str, folder_name: str) -> str | None:
    dir_path = os.path.join(results_dir, folder_name)
    if not os.path.isdir(dir_path):
        print(f"[warn] 폴더가 없습니다: {dir_path} (빈 통합 파일 생성)")
        files = []
    else:
        files = [os.path.join(dir_path, f) for f in os.listdir(dir_path) if f.lower().endswith(".csv")]

    # 출력 디렉터리: results/combine
    out_dir = os.path.join(results_dir, "combine")
    os.makedirs(out_dir, exist_ok=True)

    frames: List[pd.DataFrame] = []
    for fp in sorted(files):
        rank = parse_rank_from_filename(fp)
        # 빈 파일(0 byte) 스킵
        try:
            if os.path.getsize(fp) == 0:
                print(f"  [skip] empty file: {fp}")
                continue
        except Exception:
            pass
        try:
            df = pd.read_csv(fp, encoding="utf-8-sig")
        except UnicodeDecodeError:
            df = pd.read_csv(fp, encoding="cp949", errors="replace")
        except Exception as e:
            # pandas EmptyDataError 등 → 스킵
            print(f"  [skip] cannot read: {fp} ({e})")
            continue
        # 컬럼이 전혀 없는 경우 스킵
        if df is None or df.shape[1] == 0:
            print(f"  [skip] no columns: {fp}")
            continue
        df["rank"] = rank
        frames.append(df)

    # 병합 데이터가 없어도 빈 파일을 생성
    if not frames:
        out = pd.DataFrame()
        out_path = os.path.join(out_dir, f"{folder_name}.xlsx")
        out.to_excel(out_path, index=False)
        print(f"[ok] {folder_name}: 0 rows (no data) → {out_path}")
        return out_path

    out = pd.concat(frames, ignore_index=True)
    # rank 오름차순으로 정렬 (낮은 순위 우선)
    out = out.sort_values(["rank"], kind="stable").reset_index(drop=True)
    # case_no + address 기준으로 중복 제거
    try:
        if ("case_no" in out.columns) and ("address" in out.columns):
            before = len(out)
            out = out.drop_duplicates(subset=["case_no", "address"], keep="first").reset_index(drop=True)
            removed = before - len(out)
            if removed > 0:
                print(f"  [dedup] {removed} duplicate rows removed in {folder_name} (by case_no+address)")
        else:
            print(f"  [dedup] skip for {folder_name}: 'case_no' or 'address' column not found")
    except Exception as e:
        print(f"  [dedup] skip (error: {e})")

    # rank 컬럼은 최종 엑셀에는 포함하지 않는다
    if "rank" in out.columns:
        out = out.drop(columns=["rank"])

    out_path = os.path.join(out_dir, f"{folder_name}.xlsx")
    out.to_excel(out_path, index=False)
    print(f"[ok] {folder_name}: {len(out)} rows → {out_path}")
    return out_path


def main() -> None:
    p = argparse.ArgumentParser(description="results 하위 폴더들의 순위 CSV를 통합하여 폴더명.xlsx로 저장")
    p.add_argument("--results_dir", default="results", help="결과 루트 디렉터리")
    p.add_argument(
        "--folders",
        nargs="*",
        default=[],
        help="통합 대상 폴더 이름들 (생략 시 results 내 모든 하위 폴더 자동 대상)",
    )
    args = p.parse_args()

    # 대상 폴더 목록 결정: 인자가 없으면 results 하위 모든 폴더
    if args.folders:
        targets = args.folders
    else:
        if not os.path.isdir(args.results_dir):
            print(f"[skip] 결과 루트가 없습니다: {args.results_dir}")
            return
        targets = [
            d for d in sorted(os.listdir(args.results_dir))
            if os.path.isdir(os.path.join(args.results_dir, d))
        ]
        if not targets:
            print(f"[skip] 하위 폴더가 없습니다: {args.results_dir}")
            return

    for name in targets:
        try:
            combine_folder(args.results_dir, name)
        except Exception as e:
            print(f"[fail] {name}: {e}")


if __name__ == "__main__":
    main()


