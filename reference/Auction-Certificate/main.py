import argparse
import os
import re
import glob
import time
import signal
import pandas as pd
from multiprocessing import Pool, cpu_count
from functools import partial
from tqdm import tqdm
from ocr import SummaryPageFinder, images_from_pdf_after, ClovaOCRProcessor
from utils import Datadisk, TableProcessor

# 전역 변수로 중단 플래그 관리
interrupted = False

def signal_handler(signum, frame):
    """Ctrl+C 시그널 핸들러"""
    global interrupted
    print(f"\n[INFO] 시그널 {signum} 감지됨. 프로그램을 안전하게 종료합니다...")
    interrupted = True

# Windows 멀티프로세싱: 워커 함수는 모듈 전역에 있어야 picklable
def _preview_worker(item):
    pdf_path, out_root, secret_key, invoke_url = item
    base = os.path.splitext(os.path.basename(pdf_path))[0]
    # 폴더명은 물건번호만 사용 (예: R-001-01 [..] 주소 -> R-001_01)
    first_tok = base.split()[0]
    m = re.match(r"^((?:R|S)-\d{3})[_-]([0-9]{1,2})", first_tok)
    if m:
        item_id = f"{m.group(1)}_{int(m.group(2)):02d}"
    else:
        item_id = first_tok.replace('-', '_')[:31]
    out_dir = os.path.join(out_root, item_id)
    os.makedirs(out_dir, exist_ok=True)
    start_page = SummaryPageFinder.find_summary_start_page(
        pdf_path,
        dpi=200,
        fallback_clova=True,
        clova_secret_key=secret_key,
        clova_invoke_url=invoke_url,
    )
    pages = images_from_pdf_after(pdf_path, start_page)
    for idx, img in enumerate(pages, start=1):
        img.save(os.path.join(out_dir, f"page_{idx}.jpg"), "JPEG")
    return base, start_page, len(pages), out_dir

def _pdf_ocr_worker(args_tuple):
    """PDF 처리 워커 함수 (요약 페이지 추출 + Clova OCR)"""
    global interrupted
    if interrupted:
        return None, None
        
    pdf_path, forced_item_id, args, addr_to_item, addr_to_jibeon, jibeon_to_addr, dd, shared_addr_to_jibeons, shared_addr_item_to_jibeons, shared_consumed, shared_lock = args_tuple
    
    try:
        base = os.path.splitext(os.path.basename(pdf_path))[0]
        
        # 파일명 의존 제거: 주소만 추출해 Datadisk 매핑으로 식별
        from utils import Datadisk as _DD
        _base_id, _full_id, pdf_addr = _DD._extract_item_and_address_from_filename(pdf_path)
        norm_addr = dd._normalize_addr(pdf_addr)

        # 1. Datadisk 우선: 주소로 물건번호/지번일련번호 매칭
        final_item_id = forced_item_id or None
        final_jibeon_id = None

        # (디버그 출력 제거됨)

        if not final_item_id and norm_addr and norm_addr in addr_to_item:
            final_item_id = addr_to_item[norm_addr]

        # 주소+물건번호 복합키 우선으로 지번일련번호 선택, 없으면 주소 단독으로 선택 (락 보호)
        if norm_addr:
            try:
                with shared_lock:
                    # 1) 복합키
                    picked = None
                    lst = []
                    try:
                        key2 = f"{norm_addr}|{dd._normalize_item_id(final_item_id) if final_item_id else ''}"
                        lst = list(shared_addr_item_to_jibeons.get(key2, []))
                    except Exception:
                        lst = []
                    # 2) 복합키가 없으면 주소 단독
                    if not lst:
                        lst = list(shared_addr_to_jibeons.get(norm_addr, []))
                    picked = None
                    for j in lst:
                        if not shared_consumed.get(j, False):
                            picked = j
                            break
                    if picked:
                        shared_consumed[picked] = True
                        final_jibeon_id = picked
            except Exception:
                pass

        # 2. Datadisk에 없으면 JSON에서만 보완 (파일명/물건번호 기반 생성 금지)
        # final_item_id는 폴더 구성용으로 필요: Datadisk가 없으면 파일명 첫 토큰에서만 추정(기존 동작 유지)
        if not final_item_id:
            first_tok = os.path.splitext(os.path.basename(pdf_path))[0].split()[0]
            m = re.match(r"^((?:R|S)-\d{3})[_-]([0-9]{1,2})", first_tok)
            if m:
                final_item_id = f"{m.group(1)}_{int(m.group(2)):02d}"
            else:
                final_item_id = first_tok.replace('-', '_')[:31]

        # JSON에 기록할 지번일련번호는 Datadisk가 없을 때만 JSON 내부 추출 로직이 사용됨(이 단계에선 미상, None 유지 가능)
        # 이후 owners/gap/eul 집계 시 utils.TableProcessor가 JSON의 jibeonId만 사용

        final_item_id = dd._normalize_item_id(str(final_item_id)) if final_item_id else str(_base_id)

        # (디버그 출력 제거됨)
        
        item_dir = os.path.join(args.items_out, final_item_id)
        os.makedirs(item_dir, exist_ok=True)
        images_dir = None
        if getattr(args, "save_summary_images", False):
            images_dir = os.path.join(item_dir, "images")
            os.makedirs(images_dir, exist_ok=True)
        
        # 요약 페이지 추출
        start_page = SummaryPageFinder.find_summary_start_page(
            pdf_path,
            dpi=200,
            fallback_clova=True,
            clova_secret_key=args.secret_key,
            clova_invoke_url=args.invoke_url,
        )
        pages = images_from_pdf_after(pdf_path, start_page)
        
        if interrupted:
            return None, None
        
        # Clova OCR 처리
        ocr = ClovaOCRProcessor(secret_key=args.secret_key, invoke_url=args.invoke_url, dpi=300, enable_table=args.enable_table)
        page_objs = []
        
        for idx, img in enumerate(pages, start=1):
            if interrupted:
                return None, None
            # 옵션: 요약(및 이후) 페이지 이미지 저장
            if images_dir:
                try:
                    img.save(os.path.join(images_dir, f"page_{idx}.jpg"), "JPEG")
                except Exception:
                    pass
            # Clova OCR 호출
            resp = ocr.process_image(img, apply_sharpening=args.apply_sharpening)
            if isinstance(resp, dict) and "images" in resp:
                page_objs.append({"images": resp.get("images", [])})
        
        # JSON 저장
        json_path = os.path.join(args.json_out, f"{base}.json")
        import json as _json
        with open(json_path, "w", encoding="utf-8") as f:
            _json.dump({
                "itemId": final_item_id,
                "jibeonId": final_jibeon_id,
                "pdfName": base,
                "address": pdf_addr,
                "pages": page_objs,
            }, f, ensure_ascii=False)
        
        return final_item_id, json_path
        
    except Exception as e:
        print(f"[ERROR] PDF 처리 실패 {pdf_path}: {e}")
        return None, None

def _json_aggregate_worker(args_tuple):
    """JSON 집계 워커 함수"""
    global interrupted
    if interrupted:
        return None, None
        
    item_id, json_paths, args = args_tuple
    
    try:
        tp = TableProcessor()
        owners_df, gap_df, eul_df = tp.aggregate_from_json_paths(json_paths)
        
        out_dir = os.path.join(args.items_out, item_id)
        os.makedirs(out_dir, exist_ok=True)
        
        # owners 요약 생성
        owners_summary = None
        if owners_df is not None and not owners_df.empty:
            try:
                owners_summary = tp.summarize_owners(owners_df)
            except Exception:
                pass
            try:
                owners_to_save = owners_df.copy()
                if "OCR 주소" in owners_to_save.columns:
                    owners_to_save = owners_to_save.drop(columns=["OCR 주소"])
                owners_to_save.to_csv(os.path.join(out_dir, "owners.csv"), index=False, encoding="utf-8-sig")
            except Exception:
                owners_df.to_csv(os.path.join(out_dir, "owners.csv"), index=False, encoding="utf-8-sig")
        
        if gap_df is not None and not gap_df.empty:
            gap_csv = os.path.join(out_dir, "gapgu.csv")
            gap_xlsx = os.path.join(out_dir, "gapgu.xlsx")
            gap_df.to_csv(gap_csv, index=False, encoding="utf-8-sig")
            # XLSX 저장: 먼저 openpyxl, 실패 시 xlsxwriter
            saved = False
            try:
                with pd.ExcelWriter(gap_xlsx, engine="openpyxl") as writer:
                    gap_df.to_excel(writer, index=False, sheet_name="gapgu")
                    ws = writer.sheets["gapgu"]
                    if "순위번호" in gap_df.columns:
                        col_idx = gap_df.columns.get_loc("순위번호") + 1  # openpyxl is 1-based
                        for row_idx in range(2, len(gap_df) + 2):  # skip header
                            cell = ws.cell(row=row_idx, column=col_idx)
                            cell.number_format = "@"
                saved = True
            except Exception:
                try:
                    with pd.ExcelWriter(gap_xlsx, engine="xlsxwriter") as writer:
                        gap_df.to_excel(writer, index=False, sheet_name="gapgu")
                        wb  = writer.book
                        ws  = writer.sheets["gapgu"]
                        text_fmt = wb.add_format({"num_format": "@"})
                        if "순위번호" in gap_df.columns:
                            col_idx = gap_df.columns.get_loc("순위번호")
                            ws.set_column(col_idx, col_idx, None, text_fmt)
                    saved = True
                except Exception:
                    saved = False
        
        if eul_df is not None and not eul_df.empty:
            eul_csv = os.path.join(out_dir, "eulgu.csv")
            eul_xlsx = os.path.join(out_dir, "eulgu.xlsx")
            eul_df.to_csv(eul_csv, index=False, encoding="utf-8-sig")
            saved = False
            try:
                with pd.ExcelWriter(eul_xlsx, engine="openpyxl") as writer:
                    eul_df.to_excel(writer, index=False, sheet_name="eulgu")
                    ws = writer.sheets["eulgu"]
                    if "순위번호" in eul_df.columns:
                        col_idx = eul_df.columns.get_loc("순위번호") + 1
                        for row_idx in range(2, len(eul_df) + 2):
                            cell = ws.cell(row=row_idx, column=col_idx)
                            cell.number_format = "@"
                saved = True
            except Exception:
                try:
                    with pd.ExcelWriter(eul_xlsx, engine="xlsxwriter") as writer:
                        eul_df.to_excel(writer, index=False, sheet_name="eulgu")
                        wb  = writer.book
                        ws  = writer.sheets["eulgu"]
                        text_fmt = wb.add_format({"num_format": "@"})
                        if "순위번호" in eul_df.columns:
                            col_idx = eul_df.columns.get_loc("순위번호")
                            ws.set_column(col_idx, col_idx, None, text_fmt)
                    saved = True
                except Exception:
                    saved = False
        
        # 워커는 소유자 요약과 함께 중복 데이터 리턴 (메인에서 전역 파일로 저장)
        gap_dups = getattr(tp, "_dup_gap_records", [])
        eul_dups = getattr(tp, "_dup_eul_records", [])
        return item_id, owners_summary, gap_dups, eul_dups
        
    except Exception as e:
        print(f"[ERROR] JSON 집계 실패 {item_id}: {e}")
        return item_id, None, [], []


def main():
    global interrupted
    
    # 시그널 핸들러 등록
    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)
    
    parser = argparse.ArgumentParser(description="Datadisk 컬럼 정리/추출 실행")
    parser.add_argument("--excel", "-e", default="./data/datadisk/KB_2024_5.xlsx", help="입력 엑셀 경로")
    parser.add_argument("--cfg", "-c", default="./cfg/extract_columns.json", help="컬럼 추출 설정 JSON 경로")
    parser.add_argument("--pdfdir", default="./data/KB 2024-5 Program_등기부등본/1. 등본", help="등기부등본 PDF 폴더 경로")
    parser.add_argument("--ocr_preview", action="store_true", help="CLOVA 호출 없이 요약 이후 페이지 이미지만 추출")
    # Clova OCR 실행 옵션
    parser.add_argument("--clova", action="store_true", default=True, help="Clova OCR로 표 인식(JSON) 생성 및 집계")
    parser.add_argument("--secret_key", default=os.environ.get("CLOVA_SECRET", ""), help="Clova OCR 시크릿 키")
    parser.add_argument("--invoke_url", default=os.environ.get("CLOVA_URL", ""), help="Clova OCR Invoke URL")
    parser.add_argument("--json_out", default="./results/ocr_json", help="Clova OCR 결과(JSON) 저장 디렉터리")
    parser.add_argument("--items_out", default="./results/items", help="물건번호별 산출 저장 루트 디렉터리")
    parser.add_argument("--save_summary_images", action="store_true", help="전체 Clova OCR 실행 중 요약(및 이후) 페이지 이미지를 items_out/<itemId>/images에 JPG로 저장")
    parser.add_argument("--apply_sharpening", action="store_true", help="OCR 전 샤프닝 적용")
    parser.add_argument("--enable_table", action="store_true", help="Clova 테이블 감지 활성화")
    parser.add_argument("--max_workers", type=int, default=5, help="최대 동시 처리 프로세스 수 (Clova API 제한 고려, 기본값: 5)")
    args = parser.parse_args()

    dd = Datadisk()
    results_dir = "./results"
    os.makedirs(results_dir, exist_ok=True)

    # 사전 테스트: CLOVA 호출 전 요약 이후 페이지만 이미지로 추출
    if args.ocr_preview:
        # 지정 폴더의 PDF만 수집 (하위 폴더 미포함) - os.listdir 사용으로 [] 패턴 이슈 회피
        try:
            _entries = os.listdir(args.pdfdir)
        except FileNotFoundError:
            _entries = []
        pdf_list = [
            os.path.join(args.pdfdir, name)
            for name in _entries
            if name.lower().endswith('.pdf') and os.path.isfile(os.path.join(args.pdfdir, name))
        ]
        preview_root = os.path.join(results_dir, "ocr_preview")
        os.makedirs(preview_root, exist_ok=True)

        workers = min(max(2, cpu_count()), len(pdf_list)) or 1
        with Pool(processes=workers) as pool:
            for _ in pool.imap_unordered(
                _preview_worker, [(p, preview_root, args.secret_key, args.invoke_url) for p in pdf_list]
            ):
                pass
        # preview 전용 실행일 경우 여기서 종료
        return

    # Clova OCR 전체 실행 분기
    if args.clova:
        os.makedirs(results_dir, exist_ok=True)
        os.makedirs(args.json_out, exist_ok=True)
        os.makedirs(args.items_out, exist_ok=True)

        # ver5 기본값 폴백 (환경변수/인자 미설정 시)
        if not args.secret_key:
            args.secret_key = "ZFJlT05zZk1IZ3FvdHlLSFdWekR5U2NTb0VYdWdLd3M="
        if not args.invoke_url:
            args.invoke_url = "https://e8fcojkfeq.apigw.ntruss.com/custom/v1/44525/58d1546282e25d16c833cb89598a513261753071b687d533f66b272a3428997d/general"

        if not args.secret_key or not args.invoke_url:
            raise SystemExit("--secret_key 와 --invoke_url을 지정하거나 환경변수 CLOVA_SECRET, CLOVA_URL을 설정하세요.")

        # 1) Datadisk 로드/정리/추출 (없으면 경고 후 스킵)
        dd = Datadisk()
        addr_to_item: dict[str, str] = {}
        addr_to_jibeon: dict[str, str] = {}
        jibeon_to_addr: dict[str, str] = {}
        addr_item_to_jibeons_map: dict[tuple[str, str], list[str]] = {}
        # 주소별 지번 리스트 (Datadisk 기반) → 멀티프로세싱 공유용으로 변환 예정
        addr_to_jibeons_map: dict[str, list[str]] = {}
        try:
            base_df = dd.run(excel_path=args.excel, cfg_path=args.cfg)
            # 2) PDF 파일명에서 주소 추출 → 주소 매칭 (지정 폴더만)
            merged_df = dd.attach_registry_addresses(base_df, pdf_dir=args.pdfdir)
            # 정규화 보조 컬럼(벡터화) 추가: 주소/물건번호
            try:
                merged_df = merged_df.copy()
                merged_df["_norm_addr"] = merged_df.get("물건지 (등기부등본)", "").astype(str).map(dd._normalize_addr)
                merged_df["_norm_item"] = merged_df.get("물건번호", "").astype(str).map(dd._normalize_item_id)
            except Exception:
                pass
            
            # 3) PDF 파일명에서 지번일련번호와 주소 매핑 생성 (데이터디스크 우선)
            addr_to_jibeon, jibeon_to_addr = dd.build_jibeon_address_mapping(pdf_dir=args.pdfdir, datadisk_df=merged_df)
            
            # 4) 데이터디스크의 주소 → 물건번호 맵 생성
            for _, row in merged_df.iterrows():
                key = str(row.get("_norm_addr", "")).strip()
                item = str(row.get("_norm_item", "")).strip()
                if key and item and key not in addr_to_item:
                    addr_to_item[key] = item
            # 5) 주소별 지번 리스트 구성 (Datadisk만 사용, 파일명 보완 없음)
            for _, row in merged_df.iterrows():
                addr = str(row.get("물건지 (등기부등본)", ""))
                jibeon = str(row.get("지번일련번호", ""))
                norm = dd._normalize_addr(addr)
                if norm and jibeon:
                    addr_to_jibeons_map.setdefault(norm, []).append(jibeon)
                # 복합키 (주소+물건번호) → 지번일련번호 리스트
                n_item = str(row.get("_norm_item", "")).strip()
                n_addr = str(row.get("_norm_addr", "")).strip()
                if n_addr and n_item and jibeon:
                    addr_item_to_jibeons_map.setdefault((n_addr, n_item), []).append(jibeon)
            # 같은 주소 내 지번은 접미 숫자 기준 오름차순 정렬(안전하게 정렬)
            import re as _re
            def _suffix_num(j: str) -> int:
                try:
                    suf = _re.split(r"[_-]", j)[-1]
                    return int(suf)
                except Exception:
                    return 10**9
            for k, lst in list(addr_to_jibeons_map.items()):
                # 중복 제거 + 안정 정렬
                uniq = []
                seen = set()
                for x in lst:
                    if x not in seen:
                        uniq.append(x)
                        seen.add(x)
                addr_to_jibeons_map[k] = sorted(uniq, key=_suffix_num)
                    
        except FileNotFoundError as e:
            # Datadisk 소스가 없으면 주소 매핑을 건너뛴다(정상 폴백)
            pass

        # 지정 폴더의 PDF만 수집 (하위 폴더 미포함) - os.listdir 사용으로 [] 패턴 이슈 회피
        # 하위 폴더까지 PDF 수집
        pdf_list = []
        try:
            for root, _dirs, files in os.walk(args.pdfdir):
                for name in files:
                    if name.lower().endswith('.pdf'):
                        pdf_list.append(os.path.join(root, name))
        except FileNotFoundError:
            pdf_list = []
        
        # Datadisk 주소+물건번호 복합키 기반 PDF 선별 (사전 필터)
        try:
            from utils import Datadisk as _DDForFilter
            filtered_list = []
            # 데이터디스크의 (정규화주소, 정규화물건번호) 복합키 집합
            addr_item_pairs: set[tuple[str, str]] = set()
            for _, row in merged_df.iterrows():
                key = str(row.get("_norm_addr", "")).strip()
                item = str(row.get("_norm_item", "")).strip()
                if key and item:
                    addr_item_pairs.add((key, item))
            # 매칭 결과 로깅용 수집
            import pandas as _pd
            match_rows: list[dict] = []
            for p in pdf_list:
                _base_id, _full_id, pdf_addr = _DDForFilter._extract_item_and_address_from_filename(p)
                key = dd._normalize_addr(pdf_addr)
                item_from_name = dd._normalize_item_id(_base_id) if _base_id else ""
                matched = False
                # 1) 주소+물건번호 복합키 우선
                if key and item_from_name and (key, item_from_name) in addr_item_pairs:
                    matched = True
                    match_type = "addr+item"
                    resolved_item = item_from_name
                else:
                    # 주소 단독 허용 조건: (a) 파일명에서 물건번호를 추출하지 못했고, (b) 해당 주소가 Datadisk에서 단 하나의 물건번호만 가질 때
                    if key and not item_from_name:
                        try:
                            dd_items_for_addr = merged_df.loc[merged_df["_norm_addr"].astype(str) == key, "_norm_item"].astype(str).tolist()
                            dd_items_for_addr = sorted({s for s in dd_items_for_addr if s})
                        except Exception:
                            dd_items_for_addr = []
                        if len(dd_items_for_addr) == 1:
                            matched = True
                            match_type = "addr_only_single"
                            resolved_item = dd_items_for_addr[0]
                        else:
                            match_type = ""
                            resolved_item = ""
                    else:
                        match_type = ""
                        resolved_item = ""
                if matched:
                    # 필터 결과에 '매칭된 물건번호'를 함께 보냄 → OCR 워커에서 강제 적용
                    filtered_list.append((p, resolved_item))
                match_rows.append({
                    "pdf_path": p,
                    "pdf_base": os.path.splitext(os.path.basename(p))[0],
                    "pdf_addr": pdf_addr,
                    "norm_addr": key,
                    "pdf_item_from_name": item_from_name,
                    "dd_item_for_addr": ",".join(sorted({s for s in (merged_df.loc[merged_df["_norm_addr"].astype(str) == key, "_norm_item"].astype(str).tolist() if key else []) if s})),
                    "matched": matched,
                    "match_type": match_type,
                })
            if filtered_list:
                print(f"[INFO] 주소 매칭으로 PDF 필터링: {len(filtered_list)}/{len(pdf_list)}건 대상")
                pdf_list = filtered_list
            else:
                # 주소 매칭 결과가 없으면 전체 파일 처리로 폴백
                pass
            # 매칭 결과 CSV 저장 (데이터셋 폴더에 저장)
            try:
                match_df = _pd.DataFrame(match_rows)
                dataset_root = os.path.dirname(args.items_out)
                os.makedirs(dataset_root, exist_ok=True)
                match_csv = os.path.join(dataset_root, "pdf_match.csv")
                match_df.to_csv(match_csv, index=False, encoding="utf-8-sig")
            except Exception:
                pass
        except Exception:
            # 필터링 중 예외 발생 시 전체 파일 처리로 진행
            pass
        ocr = ClovaOCRProcessor(secret_key=args.secret_key, invoke_url=args.invoke_url, dpi=300, enable_table=args.enable_table)

        # 물건번호별 JSON 경로 수집
        item_to_jsons: dict[str, list[str]] = {}

        # 3) 요약 페이지 추출 → 4) Clova OCR → JSON 저장(물건번호 기준 폴더) - 멀티프로세싱
        print(f"[INFO] Found PDFs: {len(pdf_list)} in {args.pdfdir}")
        
        # PDF 처리용 인자 준비 (+ 주소별 지번 리스트 공유 구조)
        from multiprocessing import Manager
        mgr = Manager()
        shared_addr_to_jibeons = mgr.dict({k: v for k, v in addr_to_jibeons_map.items()})
        shared_addr_item_to_jibeons = mgr.dict({str(k[0])+"|"+str(k[1]): v for k, v in addr_item_to_jibeons_map.items()})
        shared_consumed = mgr.dict()  # jibeonId -> True
        shared_lock = mgr.Lock()

        pdf_args = [
            (
                (entry if not isinstance(entry, tuple) else entry[0]),
                (None if not isinstance(entry, tuple) else entry[1]),
                args,
                addr_to_item,
                addr_to_jibeon if 'addr_to_jibeon' in locals() else {},
                jibeon_to_addr if 'jibeon_to_addr' in locals() else {},
                dd,
                shared_addr_to_jibeons,
                shared_addr_item_to_jibeons,
                shared_consumed,
                shared_lock,
            )
            for entry in pdf_list
        ]
        
        # 멀티프로세싱으로 PDF 처리 (Clova API 제한 고려)
        num_processes = min(args.max_workers, cpu_count(), len(pdf_list))
        print(f"[INFO] PDF 처리 시작 (프로세스 수: {num_processes}, Clova API 제한: {args.max_workers})")
        
        with Pool(processes=num_processes) as pool:
            try:
                results = []
                for result in tqdm(pool.imap(_pdf_ocr_worker, pdf_args), total=len(pdf_list), desc="PDF 처리 중"):
                    if interrupted:
                        print("\n[INFO] 중단 신호 감지됨. 프로세스 정리 중...")
                        break
                    results.append(result)
            except KeyboardInterrupt:
                print("\n[INFO] 사용자에 의해 중단됨. 프로세스 정리 중...")
                pool.terminate()
                pool.join()
                raise
            finally:
                pool.close()
                pool.join()
        
        # 결과 수집
        item_to_jsons: dict[str, list[str]] = {}
        for item_id, json_path in results:
            if item_id and json_path:
                item_to_jsons.setdefault(item_id, []).append(json_path)

        # 5) 물건번호별 owners/gapgu/eulgu 집계 및 저장 - 멀티프로세싱
        print(f"[INFO] JSON 집계 시작 (물건번호: {len(item_to_jsons)}개)")
        
        # JSON 집계용 인자 준비
        json_args = [(item_id, json_paths, args) for item_id, json_paths in item_to_jsons.items()]
        
        # 멀티프로세싱으로 JSON 집계
        num_processes = min(args.max_workers, cpu_count(), len(json_args))
        print(f"[INFO] JSON 집계 시작 (프로세스 수: {num_processes})")
        
        owners_summary_parts: list[pd.DataFrame] = []
        all_gap_dups: list[dict] = []
        all_eul_dups: list[dict] = []
        with Pool(processes=num_processes) as pool:
            try:
                results = []
                for result in tqdm(pool.imap(_json_aggregate_worker, json_args), total=len(json_args), desc="JSON 집계 중"):
                    if interrupted:
                        print("\n[INFO] 중단 신호 감지됨. 프로세스 정리 중...")
                        break
                    results.append(result)
            except KeyboardInterrupt:
                print("\n[INFO] 사용자에 의해 중단됨. 프로세스 정리 중...")
                pool.terminate()
                pool.join()
                raise
            finally:
                pool.close()
                pool.join()
        
        # owners 요약 수집 및 전체 갑구/을구 데이터 수집
        all_gap_data = []
        all_eul_data = []
        for item_id, owners_summary, gap_dups, eul_dups in results:
            if owners_summary is not None and not owners_summary.empty:
                owners_summary_parts.append(owners_summary)
            try:
                if gap_dups:
                    all_gap_dups.extend(gap_dups)
                if eul_dups:
                    all_eul_dups.extend(eul_dups)
            except Exception:
                pass
        
        # 전체 갑구/을구 데이터 수집 (중복 탐지용)
        print("[INFO] 전체 갑구/을구 데이터 수집 중...")
        for item_id, owners_summary, gap_dups, eul_dups in results:
            # 각 물건번호별 갑구/을구 CSV 파일에서 데이터 읽기
            item_dir = os.path.join(args.items_out, item_id)
            gap_csv = os.path.join(item_dir, "gapgu.csv")
            eul_csv = os.path.join(item_dir, "eulgu.csv")
            
            if os.path.exists(gap_csv):
                try:
                    gap_df = pd.read_csv(gap_csv, encoding="utf-8-sig")
                    if not gap_df.empty:
                        gap_df["물건번호"] = item_id  # 물건번호 컬럼 추가
                        all_gap_data.append(gap_df)
                except Exception:
                    # 일부 파일 읽기에 실패해도 전체 플로우는 계속 진행
                    pass
            
            if os.path.exists(eul_csv):
                try:
                    eul_df = pd.read_csv(eul_csv, encoding="utf-8-sig")
                    if not eul_df.empty:
                        eul_df["물건번호"] = item_id  # 물건번호 컬럼 추가
                        all_eul_data.append(eul_df)
                except Exception:
                    pass
        
        # 전체 데이터 병합 후 중복 탐지
        if all_gap_data or all_eul_data:
            print("[INFO] 전체 데이터에서 중복 탐지 실행 중...")
            tp = TableProcessor()
            
            # 전체 갑구 데이터 병합
            if all_gap_data:
                combined_gap_df = pd.concat(all_gap_data, ignore_index=True)
            else:
                combined_gap_df = pd.DataFrame()
            
            # 전체 을구 데이터 병합
            if all_eul_data:
                combined_eul_df = pd.concat(all_eul_data, ignore_index=True)
            else:
                combined_eul_df = pd.DataFrame()
            
            # 중복 탐지 실행
            gap_dups, eul_dups = tp.detect_duplicates(combined_gap_df, combined_eul_df)
            all_gap_dups.extend(gap_dups)
            all_eul_dups.extend(eul_dups)

            # 갑구/을구 등기목적 전체 리스트 추출 및 저장 (중복 제거)
            try:
                obj_rows = []
                if not combined_gap_df.empty and "등기목적" in combined_gap_df.columns:
                    gap_objs = sorted({str(v).strip() for v in combined_gap_df["등기목적"].astype(str).tolist() if str(v).strip()})
                    obj_rows.extend({"구분": "갑구", "등기목적": o} for o in gap_objs)
                if not combined_eul_df.empty and "등기목적" in combined_eul_df.columns:
                    eul_objs = sorted({str(v).strip() for v in combined_eul_df["등기목적"].astype(str).tolist() if str(v).strip()})
                    obj_rows.extend({"구분": "을구", "등기목적": o} for o in eul_objs)
                if obj_rows:
                    obj_df = pd.DataFrame(obj_rows).drop_duplicates().sort_values(["구분", "등기목적"]).reset_index(drop=True)
                    dataset_root = os.path.dirname(args.items_out)
                    os.makedirs(dataset_root, exist_ok=True)
                    obj_path = os.path.join(dataset_root, "object_list.csv")
                    obj_df.to_csv(obj_path, index=False, encoding="utf-8-sig")
            except Exception:
                pass

        # basic_info.csv 저장 (merged_df + owners 요약)
        if 'merged_df' not in locals() or merged_df is None or merged_df.empty:
            # 초기 Datadisk 로드가 실패했을 수 있으니 재시도하여 기본 DF 생성
            try:
                base_df = dd.run(excel_path=args.excel, cfg_path=args.cfg)
                merged_df = dd.attach_registry_addresses(base_df, pdf_dir=args.pdfdir)
            except Exception as _:
                merged_df = None

        if merged_df is not None and not merged_df.empty:
            print(f"\n=== basic_info.csv 저장 시작 ===")
            print(f"merged_df shape: {merged_df.shape}")
            print(f"물건번호 개수: {merged_df['물건번호'].nunique()}")
            
            # owners 요약과 merged_df 병합
            if owners_summary_parts:
                owners_summary_all = pd.concat(owners_summary_parts, ignore_index=True)
                # 지번일련번호로 직접 병합 (주소/물건번호 사용하지 않음)
                if "지번일련번호" in owners_summary_all.columns and "지번일련번호" in merged_df.columns:
                    merged_plus = merged_df.merge(
                        owners_summary_all[["지번일련번호", "소유자", "등록번호", "최종지분", "소유자 주소"]],
                        on="지번일련번호",
                        how="left"
                    )
                else:
                    merged_plus = merged_df
            else:
                merged_plus = merged_df
            
            # 각 물건번호별로 basic_info.csv 저장
            for item_id, part in tqdm(merged_plus.groupby("물건번호", sort=True), desc="basic_info.csv 저장 중"):
                if interrupted:
                    print("\n[INFO] 중단 신호 감지됨. 저장 중단...")
                    break
                
                # basic_info.csv용 데이터프레임 생성
                basic_info_df = part.copy()
                
                # 담보물형태가 "동산담보"인 경우 특정 컬럼을 빈칸으로 설정
                if "담보물형태" in basic_info_df.columns:
                    mask = basic_info_df["담보물형태"] == "동산담보"
                    basic_info_df.loc[mask, "물건지 (DD)"] = ""
                    basic_info_df.loc[mask, "물건지 (등기부등본)"] = ""
                    basic_info_df.loc[mask, "일치여부"] = ""
                
                # 면적 변환 (제곱미터를 평으로 변환: 3.305785로 나누기)
                if "대지면적 (평)" in basic_info_df.columns:
                    basic_info_df["대지면적 (평)"] = pd.to_numeric(basic_info_df["대지면적 (평)"], errors='coerce') / 3.305785
                if "건물면적 (평)" in basic_info_df.columns:
                    basic_info_df["건물면적 (평)"] = pd.to_numeric(basic_info_df["건물면적 (평)"], errors='coerce') / 3.305785
                
                # 최종 컬럼 순서 설정 (물건번호 제외)
                final_cols = [
                    "지번일련번호",
                    "물건지 (등기부등본)",
                    "물건지 (DD)",
                    "일치여부",
                    "담보물형태",
                    "대지면적 (평)",
                    "건물면적 (평)",
                    "소유자",
                    "등록번호",
                    "최종지분",
                    "소유자 주소"
                ]
                
                # 필요한 컬럼이 없으면 빈 컬럼 추가
                for col in final_cols:
                    if col not in basic_info_df.columns:
                        basic_info_df[col] = ""
                
                # 컬럼 순서 재정렬
                basic_info_df = basic_info_df.reindex(columns=final_cols)
                
                out_dir = os.path.join(args.items_out, item_id)
                os.makedirs(out_dir, exist_ok=True)
                basic_info_path = os.path.join(out_dir, "basic_info.csv")
                basic_info_df.to_csv(basic_info_path, index=False, encoding="utf-8-sig")

        # 전역 중복 리포트 한 번에 저장 (데이터셋 폴더에 저장)
        try:
            if all_gap_dups or all_eul_dups:
                dataset_root = os.path.dirname(args.items_out)
                os.makedirs(dataset_root, exist_ok=True)
                dup_path = os.path.join(dataset_root, "duplicates.xlsx")
                with pd.ExcelWriter(dup_path, engine="openpyxl") as writer:
                    if all_gap_dups:
                        pd.DataFrame(all_gap_dups).to_excel(writer, index=False, sheet_name="gap_dups")
                    if all_eul_dups:
                        pd.DataFrame(all_eul_dups).to_excel(writer, index=False, sheet_name="eul_dups")
        except Exception:
            pass

        # Clova 전용 실행 종료
        return

    # 기존 데이터프레임과 주소 매칭/병합 후 저장
    base_df = dd.run(excel_path=args.excel, cfg_path=args.cfg)
    merged_df = dd.attach_registry_addresses(base_df, pdf_dir=args.pdfdir)
    # 최종 컬럼 순서 정렬 (물건번호 제외)
    cols = [
        "지번일련번호",
        "물건지 (등기부등본)",
        "물건지 (DD)",
        "일치여부",
        "담보물형태",
        "대지면적 (평)",
        "건물면적 (평)",
    ]
    merged_df = merged_df.reindex(columns=cols)
    merged_out = os.path.join(results_dir, "merged_df.csv")
    merged_df.to_csv(merged_out, index=False, encoding="utf-8-sig")
    print(f"Saved: {merged_out}")

    # Clova OCR이 실행되지 않은 경우에만 basic_info.csv 저장
    if not args.clova:
        # 물건번호별로 items 폴더에 basic_info.csv 저장
        items_dir = os.path.join(results_dir, "items")
        os.makedirs(items_dir, exist_ok=True)
        
        print(f"\n=== basic_info.csv 저장 시작 ===")
        print(f"merged_df shape: {merged_df.shape}")
        print(f"물건번호 개수: {merged_df['물건번호'].nunique()}")
        
        for item_id, part in merged_df.groupby("물건번호", sort=True):
            if interrupted:
                print("\n[INFO] 중단 신호 감지됨. 저장 중단...")
                break
            
            # basic_info.csv용 데이터프레임 생성
            basic_info_df = part.copy()
            
            # 담보물형태가 "동산담보"인 경우 특정 컬럼을 빈칸으로 설정
            if "담보물형태" in basic_info_df.columns:
                mask = basic_info_df["담보물형태"] == "동산담보"
                basic_info_df.loc[mask, "물건지 (DD)"] = ""
                basic_info_df.loc[mask, "물건지 (등기부등본)"] = ""
                basic_info_df.loc[mask, "일치여부"] = ""
            
            # 면적 변환 (제곱미터를 평으로 변환: 3.305785로 나누기)
            if "대지면적 (평)" in basic_info_df.columns:
                basic_info_df["대지면적 (평)"] = pd.to_numeric(basic_info_df["대지면적 (평)"], errors='coerce') / 3.305785
            if "건물면적 (평)" in basic_info_df.columns:
                basic_info_df["건물면적 (평)"] = pd.to_numeric(basic_info_df["건물면적 (평)"], errors='coerce') / 3.305785
            
            # 최종 컬럼 순서 설정 (물건번호 제외)
            final_cols = [
                "지번일련번호",
                "물건지 (등기부등본)",
                "물건지 (DD)",
                "일치여부",
                "담보물형태",
                "대지면적 (평)",
                "건물면적 (평)",
                "소유자",
                "등록번호",
                "최종지분",
                "소유자 주소"
            ]
            
            # 필요한 컬럼이 없으면 빈 컬럼 추가
            for col in final_cols:
                if col not in basic_info_df.columns:
                    basic_info_df[col] = ""
            
            # 컬럼 순서 재정렬
            basic_info_df = basic_info_df.reindex(columns=final_cols)
            
            out_dir = os.path.join(items_dir, item_id)
            os.makedirs(out_dir, exist_ok=True)
            basic_info_path = os.path.join(out_dir, "basic_info.csv")
            basic_info_df.to_csv(basic_info_path, index=False, encoding="utf-8-sig")

if __name__ == "__main__":
    # Windows 멀티프로세싱 지원
    import multiprocessing
    multiprocessing.freeze_support()
    
    now_time = time.time()
    
    try:
        main()
        print(f"Total time: {time.time() - now_time}")
    except KeyboardInterrupt:
        print("\n[INFO] 프로그램이 사용자에 의해 중단되었습니다.")
        print(f"실행 시간: {time.time() - now_time:.2f}초")
        exit(0)
    except Exception as e:
        print(f"\n[ERROR] 예상치 못한 오류 발생: {e}")
        print(f"실행 시간: {time.time() - now_time:.2f}초")
        exit(1)
