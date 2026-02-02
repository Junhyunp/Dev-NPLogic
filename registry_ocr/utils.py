import json
import sys
import os
import re
import glob
import pandas as pd
from typing import List, Dict

class Datadisk:
    """Datadisk 로더/정리/추출 유틸 클래스 (ver1.0 전용)"""

    def __init__(self, excel_path: str = "./data/datadisk/KB_2024_5.xlsx", cfg_path: str = "./cfg/extract_columns.json", integer_columns: List[str] | None = None):
        self.excel_path = excel_path
        self.cfg_path = cfg_path
        # 정수로 강제 캐스팅할 컬럼명(정규화 이전 원본 기준)
        self.integer_columns = integer_columns if integer_columns is not None else [
            "Property 일련번호",
            "등기부등본 일련번호",
        ]
        self.df: pd.DataFrame | None = None
        self.cleaned_df: pd.DataFrame | None = None
        self.result_df: pd.DataFrame | None = None

    def set_paths(self, excel_path: str | None = None, cfg_path: str | None = None) -> None:
        """경로나 파일명을 런타임에 동적으로 변경."""
        if excel_path:
            self.excel_path = excel_path
        if cfg_path:
            self.cfg_path = cfg_path

    @staticmethod
    def _clean_columns(df: pd.DataFrame) -> pd.DataFrame:
        # 줄바꿈 제거(공백 유지), 양끝 공백 트림, 연속 공백 하나로
        new_cols = [re.sub(r"\s+", " ", str(c).replace("\n", " ")).strip() for c in df.columns]
        out = df.copy()
        out.columns = new_cols
        return out

    @staticmethod
    def _normalize_name(name: str) -> str:
        return re.sub(r"\s+", " ", str(name).replace("\n", " ")).strip()

    @staticmethod
    def _match_key(name: str) -> str:
        # 매칭용 키: 공백/언더스코어/하이픈을 동일하게 취급하고 소문자화
        return re.sub(r"[\s_\-]+", " ", str(name).replace("\n", " ")).strip().lower()

    @staticmethod
    def _normalize_item_id(item_id: str) -> str:
        """물건번호 표준화: 예) R-150_1/R-150-1 -> R-150_01, S-001-1 -> S-001_01"""
        m = re.match(r"^((?:R|S)-\d+)[_-]([0-9]+)$", item_id.strip())
        if m:
            return f"{m.group(1)}_{int(m.group(2)):02d}"
        return item_id.strip()

    def _coerce_integer_columns(self) -> None:
        if self.cleaned_df is None:
            return
        for raw_name in self.integer_columns:
            norm = self._normalize_name(raw_name)
            if norm in self.cleaned_df.columns:
                ser = pd.to_numeric(self.cleaned_df[norm], errors='coerce').astype('Int64')
                self.cleaned_df[norm] = ser

    def load(self) -> pd.DataFrame:
        self.df = pd.read_excel(self.excel_path)
        return self.df

    def clean_columns(self) -> pd.DataFrame:
        if self.df is None:
            self.load()
        self.cleaned_df = self._clean_columns(self.df)
        # 숫자 컬럼 정수형으로 보정
        self._coerce_integer_columns()
        
        # Property 일련번호가 null인 데이터 삭제
        if "Property 일련번호" in self.cleaned_df.columns:
            before_count = len(self.cleaned_df)
            self.cleaned_df = self.cleaned_df.dropna(subset=["Property 일련번호"])
            after_count = len(self.cleaned_df)
            removed_count = before_count - after_count
            if removed_count > 0:
                pass
        
        return self.cleaned_df

    def _read_cfg(self) -> List[Dict]:
        with open(self.cfg_path, "r", encoding="utf-8") as f:
            cfg = json.load(f)
        return cfg.get("columns", [])

    def extract(self) -> pd.DataFrame:
        if self.cleaned_df is None:
            self.clean_columns()
        cfg_cols = self._read_cfg()
        out_cols: Dict[str, pd.Series] = {}
        # 매칭 맵 구성 (정규화된 키 -> 실제 컬럼명)
        col_match_map: Dict[str, str] = {self._match_key(c): c for c in self.cleaned_df.columns}

        for item in cfg_cols:
            target_name: str = item.get("name", "")
            headers: List[str] = item.get("headers", [])
            # 헤더 정규화(공백/_/- 동일 취급) 후 실제 컬럼명으로 매핑
            norm_headers = [self._match_key(h) for h in headers]
            present = [col_match_map[h] for h in norm_headers if h in col_match_map]

            join_with: str = str(item.get("join_with", " "))
            if len(present) == 0:
                series = pd.Series([""] * len(self.cleaned_df), index=self.cleaned_df.index)
            elif len(present) == 1:
                series = self.cleaned_df[present[0]].apply(lambda v: "" if pd.isna(v) else str(v).strip())
            else:
                # 지정된 여러 컬럼을 join_with로 결합 (숫자/NaN 안전)
                series = (
                    self.cleaned_df[present]
                    .apply(lambda col: col.map(lambda v: "" if pd.isna(v) else str(v).strip()))
                    .apply(lambda row: join_with.join([v for v in row if v]), axis=1)
                )

            # 결합 결과 내 '정수.0' 형태를 '정수'로 정규화 (예: 1.0 -> 1)
            series = series.astype(str).str.replace(r"(?<!\d)(\d+)\.0(?!\d)", r"\1", regex=True)

            out_cols[target_name] = series

        self.result_df = pd.DataFrame(out_cols)

        # 특정 컬럼 규격화: 물건번호 = 차주일련번호 + '_' + Property 일련번호(2자리)
        #                 지번일련번호 = 차주일련번호 + '_' + Property 일련번호(2자리) + '_' + 등기부등본 일련번호(2자리)
        def _pad2(col: pd.Series | None) -> pd.Series:
            if col is None:
                return pd.Series([""] * len(self.cleaned_df), index=self.cleaned_df.index)
            nums = pd.to_numeric(col, errors='coerce')
            return nums.apply(lambda x: "" if pd.isna(x) else f"{int(x):02d}")

        def _get_series(name: str) -> pd.Series | None:
            key = self._match_key(name)
            real = col_match_map.get(key)
            return self.cleaned_df[real] if real in self.cleaned_df.columns else None

        chaju = _get_series("차주일련번호")
        prop  = _get_series("Property 일련번호")
        deed  = _get_series("등기부등본 일련번호")

        if chaju is not None and prop is not None:
            mk1 = chaju.astype(str).str.strip() + "_" + _pad2(prop)
            self.result_df["물건번호"] = mk1.str.replace(r"\s+", " ", regex=True).str.strip()

        if chaju is not None and prop is not None and deed is not None:
            mk2 = chaju.astype(str).str.strip() + "_" + _pad2(prop) + "_" + _pad2(deed)
            self.result_df["지번일련번호"] = mk2.str.replace(r"\s+", " ", regex=True).str.strip()
        return self.result_df
    
    def run(self, excel_path: str | None = None, cfg_path: str | None = None) -> pd.DataFrame:
        # 실행 시 입력받은 경로로 덮어쓰기 지원
        self.set_paths(excel_path=excel_path, cfg_path=cfg_path)
        self.load()
        self.clean_columns()
        return self.extract()

    # -------- 등기부등본 PDF 파일명 기반 주소 매칭 메서드 -------- #
    @staticmethod
    def _extract_item_and_address_from_filename(filename: str) -> tuple[str, str | None, str]:
        stem = os.path.splitext(os.path.basename(filename))[0]
        tokens = stem.split()
        if not tokens:
            return "", None, ""
        id_tok = tokens[0]
        # 주소는 두 번째 토큰부터, 두 번째가 [..]이면 세 번째부터
        if len(tokens) >= 3 and tokens[1].startswith("[") and tokens[1].endswith("]"):
            addr = " ".join(tokens[2:])
        else:
            addr = " ".join(tokens[1:])
        addr = addr.strip()

        # id 정규화: R-###[_-]##([_-]##)? 모두 허용 → 두 자리 패딩, 결과는 언더스코어로
        m = re.match(r"^((?:R|S)-\d{3})[_-]([0-9]{1,2})(?:[_-]([0-9]{1,2}))?$", id_tok)
        if m:
            base_id = f"{m.group(1)}_{int(m.group(2)):02d}"
            full_norm = f"{base_id}_{int(m.group(3)):02d}" if m.group(3) else None
            return base_id, full_norm, addr

        # 마지막 시도: 하이픈을 언더스코어로 치환 후 동일 로직 적용
        id_conv = id_tok.replace("-", "_")
        m2 = re.match(r"^((?:R|S)-\d{3})_([0-9]{1,2})(?:_([0-9]{1,2}))?$", id_conv)
        if m2:
            base_id = f"{m2.group(1)}_{int(m2.group(2)):02d}"
            full_norm = f"{base_id}_{int(m2.group(3)):02d}" if m2.group(3) else None
            return base_id, full_norm, addr

        # 패턴 불일치 시 원문 반환
        return id_tok.strip(), None, addr

    def build_registry_address_df(self, pdf_dir: str) -> pd.DataFrame:
        """PDF 파일명에서 (물건번호, 물건지 (등기부등본))만 단순 추출.
        규칙: 첫 토큰이 ID, 두 번째 토큰이 [..]이면 세 번째부터 주소.
        ID는 하이픈/언더스코어 혼용 허용, 결과는 R-###_##로 표준화.
        """
        # brackets 대응: 디렉터리 경로를 glob.escape로 이스케이프하여 패턴 해석을 막음
        try:
            _escaped_dir = glob.escape(pdf_dir)
            _glob_pattern = os.path.join(_escaped_dir, "**", "*.pdf")
            files = glob.glob(_glob_pattern, recursive=True)
        except Exception:
            files = glob.glob(os.path.join(glob.escape(pdf_dir), "**", "*.pdf"), recursive=True)
        rows: list[dict] = []
        for f in files:
            base_id, _full_id, addr = self._extract_item_and_address_from_filename(f)
            if base_id:
                rows.append({
                    "물건번호": self._normalize_item_id(base_id),
                    "물건지 (등기부등본)": addr
                })
        return pd.DataFrame(rows)

    @staticmethod
    def _normalize_addr(s: str) -> str:
        if s is None:
            return ""
        return re.sub(r"\s+", "", str(s)).lower()

    def attach_registry_addresses(self, df: pd.DataFrame, pdf_dir: str) -> pd.DataFrame:
        """PDF 제목에서 추출한 '주소'만으로 매칭.
        - PDF 주소 목록을 만들고(정규화 키 → 원문 주소)
        - 원본 df의 '물건지 (DD)'를 정규화하여 키 매칭
        - 일치하면 '물건지 (등기부등본)'에 원문 주소를 채우고, '일치여부'는 O
        - 없으면 공백, '일치여부'는 X
        """
        reg_df = self.build_registry_address_df(pdf_dir)
        
        # 주소 사전: 정규화주소 → 원문주소(첫 값 우선)
        addr_map: Dict[str, str] = {}
        for addr in reg_df.get("물건지 (등기부등본)", pd.Series([], dtype=str)).astype(str).tolist():
            key = self._normalize_addr(addr)
            if key and key not in addr_map:
                addr_map[key] = addr
        

        out = df.copy()
        dd_norm = out.get("물건지 (DD)", "").astype(str).apply(self._normalize_addr)
        matched = dd_norm.map(lambda k: addr_map.get(k, ""))
        out["물건지 (등기부등본)"] = matched
        out["일치여부"] = matched.apply(lambda v: "O" if str(v).strip() else "X")
        
        return out

    def build_jibeon_address_mapping(self, pdf_dir: str, datadisk_df: pd.DataFrame = None) -> tuple[Dict[str, str], Dict[str, str]]:
        """PDF 파일명에서 지번일련번호와 주소를 추출하여 매핑 생성
        데이터디스크의 실제 지번일련번호를 우선적으로 사용
        Returns:
            tuple: (addr_to_jibeon: Dict[str, str], jibeon_to_addr: Dict[str, str])
        """
        # brackets 대응: build_registry_address_df와 동일하게 escape 적용
        files = glob.glob(os.path.join(glob.escape(pdf_dir), "**", "*.pdf"), recursive=True)
        addr_to_jibeon: Dict[str, str] = {}  # 주소 -> 지번일련번호
        jibeon_to_addr: Dict[str, str] = {}  # 지번일련번호 -> 주소
        
        # 1. 데이터디스크에서 주소-지번일련번호 매핑 생성 (우선순위 1)
        if datadisk_df is not None and not datadisk_df.empty:
            # 같은 주소에 대해 여러 지번일련번호를 모두 저장
            addr_to_jibeons: Dict[str, list[str]] = {}  # 주소 -> 지번일련번호 리스트
            
            for _, row in datadisk_df.iterrows():
                # PDF에서 추출한 주소와 매칭된 주소 사용
                pdf_addr = str(row.get("물건지 (등기부등본)", ""))
                jibeon = str(row.get("지번일련번호", ""))
                if pdf_addr and jibeon:
                    norm_addr = self._normalize_addr(pdf_addr)
                    if norm_addr:
                        if norm_addr not in addr_to_jibeons:
                            addr_to_jibeons[norm_addr] = []
                        addr_to_jibeons[norm_addr].append(jibeon)
                        jibeon_to_addr[jibeon] = pdf_addr
            
            # 각 주소에 대해 첫 번째 지번일련번호를 기본값으로 설정
            for norm_addr, jibeons in addr_to_jibeons.items():
                addr_to_jibeon[norm_addr] = jibeons[0]  # 첫 번째 지번일련번호를 기본값으로
        
        # 2. PDF 파일명에서 추가 매핑 생성 (데이터디스크에 없는 경우만)
        for f in files:
            base_id, full_id, addr = self._extract_item_and_address_from_filename(f)
            if full_id and addr:
                norm_addr = self._normalize_addr(addr)
                if norm_addr and norm_addr not in addr_to_jibeon:
                    # 데이터디스크에 없는 주소만 PDF에서 추출
                    addr_to_jibeon[norm_addr] = full_id
                    jibeon_to_addr[full_id] = addr
                    
        return addr_to_jibeon, jibeon_to_addr


# -------------------- Clova Table OCR 후처리 및 집계 --------------------

class TableProcessor:
    """Clova Table OCR JSON 후처리/분류/CSV 저장 및 물건번호 기준 집계 유틸리티"""

    # 분류 기준 키워드 (헤더 감지용)
    OWNERS_REQUIRED_COLS = ["등기명의인", "(주민)등록번호", "최종지분", "순위번호"]
    GAP_EUL_REQUIRED_COLS = ["순위번호", "등기목적", "주요등기사항"]

    # 최종 산출 컬럼 순서 (owners 테이블 스키마)
    # 등기명의인, (주민)등록번호, 최종지분, 주소, 순위번호, 물건번호, 지번일련번호
    OWNERS_TARGET_COLS = ["등기명의인", "(주민)등록번호", "최종지분", "주소", "순위번호", "물건번호", "지번일련번호"]
    GAP_EUL_TARGET_COLS = ["순위번호", "등기목적", "접수정보", "주요등기사항", "대상소유자"]
    GAP_PARSED_COLS = ["순위번호", "등기목적", "접수정보", "접수날짜", "권리자/채권자/가등기권자", "청구금액", "비고", "임금채권추정", "대상소유자", "지번번호"]
    EUL_PARSED_COLS = ["순위번호", "등기목적", "접수정보", "접수날짜", "근저당권자/전세권자/채권자/지상권자/임차권자", "채권최고액/전세금/임차보증금", "채무자", "담보종류", "공장저당", "대상소유자", "지번번호", "비고"]

    # 갑구/을구 키워드
    GAP_KEYWORDS = [
        "가압류", "압류", "임의경매개시결정", "강제경매개시결정", "보전처분", "가등기", "소유권이전청구권가등 기", "약정/금지사항/환매특 약"
    ]
    EUL_KEYWORDS = [
        "가처분", "근저당권설정", "전세권", "임차권", "근저당권일부이전", "사해행위취소의 소", "지상권설정"
    ]

    def __init__(self, output_dir: str = "."):
        self.output_dir = output_dir
        os.makedirs(self.output_dir, exist_ok=True)
        self.OWNERS_CSV = os.path.join(self.output_dir, 'owners_merged.csv')
        self.GAPGU_CSV = os.path.join(self.output_dir, 'gapgu_merged.csv')
        self.EULGU_CSV = os.path.join(self.output_dir, 'eulgu_merged.csv')
        # 중복 내용 보고서 출력 파일 및 누적 버퍼
        self.DUP_XLSX = os.path.join(self.output_dir, 'duplicates.xlsx')
        self._dup_gap_records: list[dict] = []
        self._dup_eul_records: list[dict] = []
        # 디버그 설정(병합 시 순위번호 확인용)
        self.debug = True
        self.debug_merge_limit = 50

    @staticmethod
    def normalize(s: str) -> str:
        if s is None:
            return ''
        return str(s).strip()

    @staticmethod
    def _normalize_rank_with_prev(s: str) -> str:
        """순위번호 정규화: "1 (전 8)" 같은 표기를 "1"로 변환.
        - 하이픈 서브표기("3-1")는 그대로 유지
        - 괄호 내 공백/형태 변주 허용: (전8), ( 전 8 ), (前 8) 등
        - 숫자가 맨 앞에 있지 않으면 원문 유지
        """
        try:
            text = str(s).strip()
            if not text:
                return text
            # 하이픈 서브 표기는 건드리지 않음
            if '-' in text:
                return text
            # "1 (전 8)" 패턴 감지되면 선행 숫자만 반환
            import re as _re
            if _re.search(r"\(\s*[전前]", text):
                m = _re.match(r"^\s*(\d+)", text)
                if m:
                    return m.group(1)
            return text
        except Exception:
            return str(s)

    # ----------------------- 갑구/을구 파싱 함수들 -----------------------
    @staticmethod
    def _extract_date(s: str) -> str:
        """접수정보에서 날짜 추출 (YYYY-MM-DD 형식)"""
        if not s:
            return ""
        date_pat = re.compile(r"(\d{4})년\s*(\d{1,2})월\s*(\d{1,2})일")
        m = date_pat.search(str(s))
        if not m:
            return ""
        y, mth, d = m.groups()
        return f"{y}-{int(mth):02d}-{int(d):02d}"

    @staticmethod
    def _extract_receipt_serial(s: str) -> str:
        """접수정보에서 제xxxxx호 일련번호 숫자만 추출"""
        if not s:
            return ""
        m = re.search(r"제\s*(\d+)\s*호", str(s))
        return m.group(1) if m else ""

    @staticmethod
    def _extract_receipt_date(s: str) -> str:
        """접수정보에서 접수날짜만 추출 (YYYY-MM-DD 형태)"""
        if not s:
            return ""
        return TableProcessor._extract_date(s)

    @staticmethod
    def _build_receipt_key(s: str) -> str:
        """날짜(YYYY-MM-DD) + 일련번호 조합 키. 없으면 가능한 정보만 사용"""
        date = TableProcessor._extract_date(s)
        serial = TableProcessor._extract_receipt_serial(s)
        if date and serial:
            return f"{date}|{serial}"
        if date:
            return date
        if serial:
            return f"|{serial}"
        return str(s or "").strip()

    @staticmethod
    def _extract_creditor(s: str) -> str:
        """권리자/채권자/근저당권자/전세권자/가등기권자/지상권자 추출 - 라벨이 있는 경우만 추출"""
        text = str(s or "")
        
        # "근저당권자" 등의 라벨 뒤부터 다음 라벨(채권최고액, 전세금, 임차보증금, 채무자 등) 전까지 추출
        label_pat = re.compile(
            r"(?:근저당권자|전세권자|권리자|채권자|가등기권자|지상권자|임차권자)\s*"  # 라벨
            r"([^\n]+?)"  # 라벨 뒤 텍스트 (탐욕적이지 않게)
            r"(?=\s*(?:채권최고액|전세금|임차보증금|채무자|담보종류|공장저당|$))"  # 다음 라벨 전까지
        )
        m = label_pat.search(text)
        if m:
            result = m.group(1).strip()
            # 금액 제거만 수행 (패턴 매칭 없이)
            result = re.sub(r"금\s*[0-9,]+\s*원", "", result).strip()
            if result:
                return result
        
        # 라벨이 없으면 빈 문자열 반환 (약정/금지사항 등은 권리자가 없음)
        return ""

    @staticmethod
    def _extract_amount(s: str) -> str:
        """주요등기사항에서 청구금액/채권최고액/전세금 추출"""
        text = str(s or "")
        # 줄바꿈/복수 공백을 단일 공백으로 정규화
        norm = re.sub(r"\s+", " ", text)

        # 콤마 그룹 보정 후 숫자만 남기는 헬퍼
        def _normalize_amount_token(token: str) -> str:
            raw = str(token or "").strip()
            if not raw:
                return ""
            cleaned = re.sub(r"[^0-9,]", "", raw)
            if "," in cleaned:
                parts = cleaned.split(",")
                last = parts[-1]
                if 0 < len(last) < 3:
                    parts[-1] = last + ("0" * (3 - len(last)))
                cleaned = "".join(parts)
            else:
                cleaned = re.sub(r"[^0-9]", "", cleaned)
            return cleaned

        # 0) USD 케이스 우선 처리
        # 0-1) 한글 표기 금액 + (불|달러) → "<한글숫자> 달러"
        m = re.search(r"(?:미합중국법화|미국법화|미합중국화폐|미국화폐|미화)\s*([가-힣]+)\s*(?:불|달러)", norm)
        if m:
            return f"{m.group(1)} 달러"
        # 0-2) (미화)? 금액 + (달러|불) → "미화 <금액>달러" 또는 "<금액>달러"
        m = re.search(r"(미화)?\s*(?:금)?\s*([0-9][0-9,]{2,})\s*(?:달러|불)", norm)
        if m:
            prefix = "미화 " if m.group(1) else ""
            return f"{prefix}{m.group(2)}달러"

        # 1) 라벨 + 숫자 (원 미표기) → 쉼표 포함 문자열 반환
        for label in ["청구금액", "채권최고액", "전세금"]:
            m = re.search(rf"{label}\s*(?:금)?\s*([0-9][0-9,]{{3,}})(?!\s*원)", norm)
            if m:
                digits = _normalize_amount_token(m.group(1))
                if digits:
                    try:
                        return f"{int(digits):,}"
                    except Exception:
                        return digits
                return ""

        # 2) 라벨 + 숫자 + 원 → 쉼표 포함 문자열 반환
        for label in ["청구금액", "채권최고액", "전세금"]:
            m = re.search(rf"{label}\s*(?:금)?\s*([0-9][0-9,]{{3,}})\s*원", norm)
            if m:
                digits = _normalize_amount_token(m.group(1))
                if digits:
                    try:
                        return f"{int(digits):,}"
                    except Exception:
                        return digits
                return ""

        # 3) 본문 내 금액(원) 후보에서 최대값 선택 → 쉼표 포함 문자열 반환
        nums = re.findall(r"([0-9][0-9,]{3,})\s*원", norm)
        if nums:
            vals = []
            for n in nums:
                try:
                    vals.append(int(_normalize_amount_token(n)))
                except Exception:
                    pass
            if vals:
                try:
                    return f"{max(vals):,}"
                except Exception:
                    return str(max(vals))
        # 4) 기타 쉼표 포함 큰 숫자 중 최대값 (마지막 폴백)
        nums = re.findall(r"([0-9][0-9,]{3,})", norm)
        if nums:
            vals = []
            for n in nums:
                try:
                    vals.append(int(_normalize_amount_token(n)))
                except Exception:
                    pass
            if vals:
                try:
                    return f"{max(vals):,}"
                except Exception:
                    return str(max(vals))
        return ""

    def parse_gap(self, df: pd.DataFrame) -> pd.DataFrame:
        """갑구 테이블을 파싱하여 최종 컬럼 형태로 변환"""
        if df is None or df.empty:
            return pd.DataFrame(columns=self.GAP_PARSED_COLS)
        
        out = df.copy()
        
        # 중복된 "주요등기사항" 컬럼들을 모두 합침 (주요등기사항, 주요등기사항.1, 주요등기사항.2 등)
        major_cols = [c for c in out.columns if c == "주요등기사항" or c.startswith("주요등기사항.")]
        
        if major_cols:
            def _combine_major_cols(row):
                parts = []
                for col in major_cols:
                    val = str(row.get(col, "")).strip()
                    if val and val.lower() not in ("", "nan", "none"):
                        parts.append(val)
                return " ".join(parts)
            out["_combined_주요등기사항"] = out.apply(_combine_major_cols, axis=1)
        else:
            out["_combined_주요등기사항"] = out.get("주요등기사항", "").astype(str)
        
        # 기존 컬럼 유지 및 새 컬럼 추가
        # 접수정보는 원본 그대로 유지
        out["접수정보"] = out.get("접수정보", "").astype(str)

        # 같은 지번일련번호에서 들어온 행은 병합하지 않기 위한 출처 지번 보존
        try:
            def _first_jibeon_eul(v):
                try:
                    if isinstance(v, (list, tuple)):
                        return str(v[0]).strip() if v else ""
                    return str(v).strip()
                except Exception:
                    return str(v)
            if "지번일련번호" in out.columns:
                out["_src_jibeon"] = out["지번일련번호"].apply(_first_jibeon_eul)
            else:
                out["_src_jibeon"] = ""
        except Exception:
            out["_src_jibeon"] = ""

        # 순위번호 정규화: "1 (전 8)" → "1"
        if "순위번호" in out.columns:
            out["순위번호"] = out["순위번호"].astype(str).map(self._normalize_rank_with_prev)

        # 같은 지번일련번호에서 온 행은 병합하지 않기 위한 출처 지번 보존
        def _first_jibeon(v):
            try:
                if isinstance(v, (list, tuple)):
                    return str(v[0]).strip() if v else ""
                return str(v).strip()
            except Exception:
                return str(v)
        out["_src_jibeon"] = out.get("지번일련번호", "").apply(_first_jibeon) if "지번일련번호" in out.columns else ""
        
        # 접수날짜는 접수정보에서 추출 (YYYY-MM-DD 형태)
        out["접수날짜"] = out["접수정보"].apply(self._extract_receipt_date)
        # _접수키는 접수정보 기반으로 생성
        out["_접수키"] = out["접수정보"].apply(self._build_receipt_key)

        # 병합 전에 메인행-서브행 소속 메인 접수키를 명시적으로 기록
        try:
            out["_orig_order"] = range(len(out))
            def _parse_rank0(s: str) -> tuple[float, float]:
                try:
                    t = str(s or "").strip()
                    m = re.match(r"^(\d+)(?:-(\d+))?$", t)
                    if not m:
                        return float('inf'), float('inf')
                    base = float(m.group(1))
                    sub = float(m.group(2)) if m.group(2) else float('inf')
                    return base, sub
                except Exception:
                    return float('inf'), float('inf')
            rp0 = out.get("순위번호", "").apply(_parse_rank0)
            out["_rank_base"] = rp0.apply(lambda x: x[0])
            out["_rank_sub"] = rp0.apply(lambda x: x[1])
            out["_is_sub"] = (out["_rank_sub"] != float('inf')).astype(int)

            out["메인행_접수정보"] = ""
            out["메인행_접수키"] = ""
            # 메인행 대상소유자 앵커도 저장하여 향후 매칭에 사용
            out["메인행_대상소유자"] = ""
            for i in out.index:
                if out.loc[i, "_is_sub"] != 1:
                    continue
                sub_base = out.loc[i, "_rank_base"]
                sub_order = out.loc[i, "_orig_order"]
                sub_owner = str(out.loc[i, "대상소유자"]).strip()
                cands = out[(out["_is_sub"] == 0) & (out["_rank_base"] == sub_base) & (out["_orig_order"] < sub_order)]
                if cands.empty:
                    continue
                owner_match = cands[cands["대상소유자"].astype(str).str.strip() == sub_owner]
                if not owner_match.empty:
                    main_row = owner_match.loc[owner_match["_orig_order"].idxmax()]
                else:
                    main_row = cands.loc[cands["_orig_order"].idxmax()]
                main_rec = str(main_row.get("접수정보", ""))
                out.at[i, "메인행_접수정보"] = main_rec
                try:
                    out.at[i, "메인행_접수키"] = TableProcessor._build_receipt_key(main_rec)
                except Exception:
                    out.at[i, "메인행_접수키"] = ""
                # 메인행 대상소유자 앵커 저장
                try:
                    out.at[i, "메인행_대상소유자"] = str(main_row.get("대상소유자", "")).replace("\u200B", "").strip()
                except Exception:
                    pass
        except Exception:
            pass
        # 합쳐진 주요등기사항에서 권리자와 금액 추출
        out["권리자/채권자/가등기권자"] = out["_combined_주요등기사항"].apply(self._extract_creditor)
        out["청구금액"] = out["_combined_주요등기사항"].apply(self._extract_amount)
        # 표시용 쉼표 제거(문자열 유지)
        try:
            if "청구금액" in out.columns:
                out["청구금액"] = out["청구금액"].astype(str).str.replace(",", "", regex=False)
        except Exception:
            pass
        # 권리자/채권자/가등기권자 컬럼의 띄어쓰기 제거 (병합 전)
        out["권리자/채권자/가등기권자"] = out["권리자/채권자/가등기권자"].astype(str).str.replace(r"\s+", "", regex=True)
        # 원본 주요등기사항 컬럼을 합쳐진 버전으로 업데이트 (나중에 출력용)
        out["주요등기사항"] = out["_combined_주요등기사항"]

        # [규칙] 을구: 등기목적이 '요역지지역권'이면 접수정보/권리자/금액(및 접수번호)은 비워둠
        try:
            if "등기목적" in out.columns:
                _p = out["등기목적"].astype(str).str.replace(r"\s+", "", regex=True)
                _mask_yoryeok = _p.str.contains("요역지지역권", na=False)
                if _mask_yoryeok.any():
                    for _c in [
                        "접수정보",
                        "근저당권자/전세권자/채권자/지상권자/임차권자",
                        "채권최고액/전세금/임차보증금",
                        "접수번호",
                    ]:
                        if _c in out.columns:
                            out.loc[_mask_yoryeok, _c] = ""
        except Exception:
            pass

        # 접수정보 기준 병합: 같은 (물건번호, 등기목적, 접수정보) 묶음으로 통합하고 순위번호는 최소 선택
        def _first_non_empty(series: pd.Series) -> str:
            for v in series.astype(str).tolist():
                s = str(v).strip()
                if s:
                    return s
            return ""
        # 1) 표준 임시 병합키 생성: _권리자, _금액 (갑구)
        if "권리자/채권자/가등기권자" in out.columns:
            out["_권리자"] = out["권리자/채권자/가등기권자"].astype(str).str.replace(r"\s+", "", regex=True)
        if "청구금액" in out.columns:
            out["_금액"] = out["청구금액"].astype(str).map(lambda x: re.sub(r"[^0-9]", "", x))

        # 2) 병합 키 선택 (요구사항 2): 접수정보 + 대상소유자(공백제거) 기반 병합
        # 대상소유자 정규화 키 준비
        try:
            out["_owner_key"] = out.get("대상소유자", "").astype(str).str.replace(r"\s+", "", regex=True)
        except Exception:
            out["_owner_key"] = out.get("대상소유자", "")
        merge_keys = [c for c in ["물건번호", "_접수키", "_owner_key"] if c in out.columns]
        if merge_keys:
            # [DEBUG] 그룹바이 직전 특정 키 카운트 및 상위 그룹 빈도 출력
            try:
                import os as _os
                _dbg_item = _os.environ.get("DBG_GAP_ITEM", "R-019_01")
                _dbg_key = _os.environ.get("DBG_GAP_KEY", "2025-06-05|2815917")
                if {"물건번호", "_접수키"}.issubset(out.columns):
                    _m = (out["물건번호"].astype(str) == str(_dbg_item)) & (out["_접수키"].astype(str) == str(_dbg_key))
                    pass
                    try:
                        _dup = (
                            out.groupby(["물건번호", "_접수키"], dropna=False)
                               .size()
                               .reset_index(name="cnt")
                               .sort_values("cnt", ascending=False)
                        )
                        pass
                    except Exception:
                        pass
            except Exception:
                pass
            # [DEBUG] 각 행의 접수키/접수정보/순위번호를 그대로 출력 (상위 100행)
            try:
                cols = [c for c in ["물건번호", "순위번호", "접수정보", "_접수키", "등기목적", "권리자/채권자/가등기권자", "청구금액", "대상소유자"] if c in out.columns]
                if cols:
                    pass
            except Exception:
                pass
            # [DEBUG] 동일 내용인데 _접수키가 서로 달라 병합 누락될 후보를 최대 10건 출력
            try:
                dbg = out.copy()
                dbg["_dbg_대상"] = dbg.get("대상소유자", "").astype(str).str.replace(r"\s+", "", regex=True)
                dbg_keys = [c for c in ["물건번호", "등기목적", "_권리자", "_금액", "_dbg_대상"] if c in dbg.columns]
                printed = 0
                if dbg_keys:
                    for _, gg in dbg.groupby(dbg_keys, dropna=False):
                        if len(gg) <= 1:
                            continue
                        try:
                            nunq = gg.get("_접수키", "").nunique()
                        except Exception:
                            nunq = 0
                        if nunq > 1:
                            pass
                            printed += 1
                            if printed >= 10:
                                break

                # 보조 로그: 권리자만 달라 병합 후보에서 빠지는 경우 출력(등기목적/금액/대상 동일)
                printed2 = 0
                weak_keys = [c for c in ["물건번호", "등기목적", "_금액", "_dbg_대상"] if c in dbg.columns]
                if weak_keys:
                    for _, gg in dbg.groupby(weak_keys, dropna=False):
                        if len(gg) <= 1:
                            continue
                        # 권리자 표기만 다른 경우(공백 제거 기준)
                        try:
                            nunq_owner = gg.get("_권리자", "").nunique()
                        except Exception:
                            nunq_owner = 0
                        if nunq_owner > 1:
                            pass
                            printed2 += 1
                            if printed2 >= 20:
                                break
            except Exception:
                pass
            agg_map: dict[str, any] = {}
            # 집계 시 가장 이른 접수정보/접수날짜를 선택하도록 보조 함수 정의
            def _pick_earliest_receipt(series: pd.Series) -> str:
                best_tuple = None
                best_text = ""
                for v in series.astype(str).tolist():
                    s = str(v).strip()
                    if not s:
                        continue
                    d = TableProcessor._extract_date(s)
                    serial = TableProcessor._extract_receipt_serial(s)
                    key = (
                        pd.to_datetime(d, errors="coerce"),
                        pd.to_numeric(serial, errors="coerce")
                    )
                    if best_tuple is None or (
                        (pd.notna(key[0]) and (pd.isna(best_tuple[0]) or key[0] < best_tuple[0])) or
                        (pd.isna(key[0]) and pd.isna(best_tuple[0]) and pd.notna(key[1]) and (pd.isna(best_tuple[1]) or key[1] < best_tuple[1]))
                    ):
                        best_tuple = key
                        best_text = s
                return best_text

            def _min_date_str(series: pd.Series) -> str:
                dt = pd.to_datetime(series.astype(str), errors="coerce")
                if dt.notna().any():
                    return str(dt.min().date())
                return ""
            if "순위번호" in out.columns:
                def _pick_best_rank(series: pd.Series) -> str:
                    best_key = (float('inf'), float('inf'))
                    best_text = ""
                    for v in series.astype(str).tolist():
                        s = str(v).strip()
                        if not s:
                            continue
                        m = re.match(r"^(\d+)(?:-(\d+))?$", s)
                        if m:
                            base = float(m.group(1))
                            sub = float(m.group(2)) if m.group(2) else float('inf')
                        else:
                            # 숫자로만 이루어진 형태가 아니면 뒤로 미룸
                            base, sub = float('inf'), float('inf')
                        key = (base, sub)
                        if key < best_key:
                            best_key = key
                            best_text = s
                    # 반환은 원본 텍스트(하이픈 유지)
                    return best_text
                agg_map["순위번호"] = _pick_best_rank
            # 서브행 소속 메인 힌트 보존
            if "메인행_접수키" in out.columns:
                agg_map["메인행_접수키"] = _first_non_empty
            # 접수정보/접수날짜는 가장 이른 값으로 집계
            if "접수정보" in out.columns:
                agg_map["접수정보"] = _pick_earliest_receipt
            if "접수날짜" in out.columns:
                agg_map["접수날짜"] = _min_date_str
            # 병합키로 사용한 컬럼은 집계에서 제외(중복 충돌 방지)
            mk_set = set(merge_keys)
            for c in ["등기목적", "권리자/채권자/가등기권자", "청구금액", "대상소유자", "주요등기사항", "_권리자", "_금액"]:
                if c in out.columns and c not in mk_set:
                    agg_map[c] = _first_non_empty
            if "지번일련번호" in out.columns:
                # 병합 동안 원본 지번일련번호들을 리스트로 유지
                def _collect_ids(s: pd.Series) -> list[str]:
                    vals = [str(v).strip() for v in s.astype(str).tolist() if str(v).strip()]
                    ids: set[str] = set()
                    for v in vals:
                        for part in str(v).split(','):
                            p = part.strip()
                            if p:
                                ids.add(p)
                    return sorted(ids)
                agg_map["지번일련번호"] = _collect_ids
            
            out = (
                out.groupby(merge_keys, dropna=False)
                   .agg(agg_map)
                   .reset_index()
            )
            # [DEBUG] 그룹바이 후 특정 키 카운트 출력
            try:
                import os as _os
                _dbg_item = _os.environ.get("DBG_GAP_ITEM", "R-019_01")
                _dbg_key = _os.environ.get("DBG_GAP_KEY", "2025-06-05|2815917")
                if {"물건번호", "_접수키"}.issubset(out.columns):
                    m2 = (out["물건번호"].astype(str) == str(_dbg_item)) & (out["_접수키"].astype(str) == str(_dbg_key))
                    pass
            except Exception:
                pass
            

            # 집계된 지번일련번호에서 지번번호 라벨 생성 (예: '01, '02)
            try:
                if "지번일련번호" in out.columns:
                    def _label_from_ids(val) -> str:
                        # val: list[str] 또는 콤마 문자열
                        ids: list[str] = []
                        if isinstance(val, (list, tuple)):
                            ids = [str(v).strip() for v in val if str(v).strip()]
                        else:
                            ids = [p.strip() for p in str(val).split(',') if p.strip()]
                        # 접미 2자리 추출 후 정렬/중복 제거
                        import re as _re
                        def _suf(v: str) -> str:
                            last = _re.split(r"[_-]", v)[-1]
                            return last.zfill(2) if last.isdigit() else last
                        labels = [f"'{_suf(v)}" for v in ids if v]
                        def _ord(lbl: str) -> int:
                            m = re.match(r"^'?([0-9]{2})", lbl)
                            return int(m.group(1)) if m else 10**9
                        uniq = sorted(dict.fromkeys(labels), key=_ord)
                        return ", ".join(uniq)
                    out["지번번호"] = out["지번일련번호"].apply(_label_from_ids)
            except Exception:
                pass
            
            # 임시 표준키는 이후 라벨 조인까지 사용하므로 지금은 유지

        # (요구사항 3) 접수일 상이/지번번호 라벨 조인 제거
        out["지번번호"] = out.get("지번번호", "")
        if "접수일 상이" in out.columns:
            out = out.drop(columns=["접수일 상이"], errors="ignore")
        
        # 빈 컬럼 추가 (갑구용)
        out["비고"] = ""
        out["임금채권추정"] = ""
        # 특수 처리: 약정/금지/환매특약 계열은 금액/권리자 비우고 주요등기사항을 비고에 이관
        try:
            def _is_special_purpose(p: str) -> bool:
                s = re.sub(r"\s+", "", str(p or ""))
                # "약정/금지사항/환매특 약" 등 공백/변형까지 포괄
                return ("환매특약" in s) or ("약정" in s and "금지" in s)
            if "등기목적" in out.columns:
                _mask = out["등기목적"].apply(_is_special_purpose)
                if _mask.any():
                    if "주요등기사항" in out.columns:
                        out.loc[_mask, "비고"] = out.loc[_mask, "주요등기사항"].astype(str).map(lambda x: str(x).strip())
                    if "청구금액" in out.columns:
                        out.loc[_mask, "청구금액"] = ""
                    if "권리자/채권자/가등기권자" in out.columns:
                        out.loc[_mask, "권리자/채권자/가등기권자"] = ""
        except Exception:
            pass
        # 지번번호 = 지번일련번호의 마지막 세그먼트(R-001_01_01 -> 01)
        def _suffix_from_jibeon(val: str) -> str:
            try:
                s = str(val).strip()
                if not s:
                    return ""
                # 구분자는 '_' 또는 '-' 모두 허용
                parts = re.split(r"[_-]", s)
                return parts[-1] if parts else ""
            except Exception:
                return ""
        # 집계 이후 내용 기준 축약(조건부):
        # - 같은 지번(원천)이면 절대 축약하지 않음
        # - 서로 다른 지번에서 온 동일 내용(물건번호/등기목적/권리자/채권자/가등기권자/청구금액/주요등기사항/대상소유자)은 대표 1행으로 축약
        #   대표: 가장 이른 접수날짜. 대표 외 지번은 '접수일 상이' 라벨에만 기록
        content_cols_gap = [c for c in [
            "물건번호", "등기목적", "권리자/채권자/가등기권자", "청구금액", "주요등기사항", "대상소유자"
        ] if c in out.columns]
        if content_cols_gap and "지번일련번호" in out.columns:
            # 그룹 키 정규화
            tmp_g = out.copy()
            for cc in content_cols_gap:
                tmp_g[cc] = tmp_g[cc].fillna("").astype(str).str.strip().replace({"nan": "", "None": ""})
                # 주요등기사항은 띄어쓰기도 제거 (병합 기준 - 출력용 원본은 유지)
                if cc == "주요등기사항":
                    tmp_g[cc] = tmp_g[cc].str.replace(r"\s+", "", regex=True)

            def _suf(v: str) -> str:
                last = re.split(r"[_-]", str(v).strip())[-1]
                return last.zfill(2) if last.isdigit() else last

            merged_records_gap: list[dict] = []
            group_num = 0
            for group_key, g in tmp_g.groupby(content_cols_gap, dropna=False):
                group_num += 1
                g = out.loc[g.index]
                
                # 동일 지번 원천만이면 축약 금지
                unique_src = set(str(x).strip() for x in g.get("_src_jibeon", "").astype(str).tolist()) if "_src_jibeon" in g.columns else set()
                if len(g) <= 1 or len(unique_src) <= 1:
                    merged_records_gap.extend(g.to_dict(orient="records"))
                    continue

                infos = []  # (row_idx, suf, rec_info, rec_date)
                for ridx, r in g.iterrows():
                    ids = r.get("지번일련번호", "")
                    id_list = ids if isinstance(ids, (list, tuple)) else [ids]
                    rec_info = str(r.get("접수정보", "")).strip()
                    rec_date = pd.to_datetime(str(r.get("접수날짜", "")).strip(), errors="coerce")
                    for one in id_list:
                        s = _suf(one)
                        if s:
                            infos.append((ridx, s, rec_info, rec_date))

                valid = [(ridx, s, info, d) for (ridx, s, info, d) in infos if pd.notna(d)]
                if valid:
                    ridx_rep, s_rep, info_rep, date_rep = sorted(valid, key=lambda x: x[3])[0]
                    rep = g.loc[ridx_rep].copy()
                    rep["접수정보"] = info_rep
                    rep["접수날짜"] = date_rep.strftime("%Y-%m-%d")
                else:
                    ridx_rep, s_rep, info_rep, date_rep = (infos[0] if infos else (g.index[0], "", str(g.iloc[0].get("접수정보", "")), pd.NaT))
                    rep = g.loc[ridx_rep].copy()

                # 지번 라벨
                all_labels = []
                uniq_suffixes = set()
                for (_, s, _, _) in infos:
                    if s:
                        uniq_suffixes.add(s)
                        all_labels.append(f"'{s}")
                def _ord(lbl: str) -> int:
                    m = re.match(r"^'?(\d{2})", lbl)
                    return int(m.group(1)) if m else 10**9
                rep["지번번호"] = ", ".join(sorted(dict.fromkeys(all_labels), key=_ord))
                
                # 접수일 상이: 지번 2개 이상일 때만
                if len(uniq_suffixes) <= 1:
                    rep["접수일 상이"] = ""
                else:
                    others = []
                    for (_, s, _, d) in infos:
                        if not s:
                            continue
                        if valid and pd.notna(d) and d == date_rep:
                            continue
                        if not valid and s == s_rep:
                            continue
                        others.append(f"'{s}")
                    rep["접수일 상이"] = ", ".join(sorted(dict.fromkeys(others), key=_ord)) if others else ""

                if "지번일련번호" in rep.index:
                    rep["지번일련번호"] = ""
                merged_records_gap.append(rep.to_dict())

            out = pd.DataFrame(merged_records_gap)

        # 집계 이후에는 원본 계산에서 조인된 결과만 사용. 지번일련번호 원본은 제거
        out = out.drop(columns=["지번일련번호"], errors="ignore")
        
        # 대상소유자 컬럼이 없으면 빈 값으로 채우기
        if "대상소유자" not in out.columns:
            out["대상소유자"] = ""
        
        # (원본 조인 결과를 사용하므로 추가 라벨 생성/덮어쓰기는 수행하지 않음)

        # (요구사항 3) 정렬: 접수날짜 → 접수 일련번호 (로컬순위번호는 제외)
        out["_접수날짜_dt"] = pd.to_datetime(out.get("접수날짜", ""), errors="coerce")
        def _serial_ord_gap(s: str) -> int:
            v = TableProcessor._extract_receipt_serial(str(s))
            return int(v) if v.isdigit() else 10**9
        out["_serial_int"] = out.get("접수정보", "").astype(str).map(_serial_ord_gap)
        out = out.sort_values(["_접수날짜_dt", "_serial_int"], na_position="last")

        # ===== 갑구 재매김을 가압류/압류/기타 3그룹으로 분리 수행 =====
        def _parse_rank_gap(rank_str: str) -> tuple[float, float]:
            try:
                s = str(rank_str).strip()
                if not s:
                    return float('inf'), float('inf')
                if '-' in s:
                    b, sub = s.split('-', 1)
                    base = pd.to_numeric(b, errors='coerce')
                    subn = pd.to_numeric(sub, errors='coerce')
                    base = float(base) if pd.notna(base) else float('inf')
                    subn = float(subn) if pd.notna(subn) else float('inf')
                    return base, subn
                base = pd.to_numeric(s, errors='coerce')
                base = float(base) if pd.notna(base) else float('inf')
                return base, float('inf')
            except Exception:
                return float('inf'), float('inf')

        def _renumber_one_group(g: pd.DataFrame) -> pd.DataFrame:
            if g is None or g.empty:
                return g
            # 디버그 토글: 매칭키 출력
            try:
                import os as _os
                def _env_true(name: str) -> bool:
                    v = str(_os.environ.get(name, "0")).strip().lower()
                    return v in ("1", "true", "yes", "on")
                _DBG_MATCH_KEYS = _env_true("DBG_MATCH_KEYS")
            except Exception:
                _DBG_MATCH_KEYS = False
            rp = g.get("순위번호", "").apply(_parse_rank_gap)
            g["_rank_base"] = rp.apply(lambda x: x[0])
            g["_rank_sub"] = rp.apply(lambda x: x[1])
            g["_is_sub"] = (g["_rank_sub"] != float('inf')).astype(int)
            g["_orig_order"] = range(len(g))
            g["_date_ord"] = pd.to_datetime(g.get("접수날짜", ""), errors="coerce")
            # 접수번호(일련번호) 정렬용 보조 컬럼
            try:
                g["_receipt_serial"] = pd.to_numeric(
                    g.get("접수정보", "").astype(str).map(TableProcessor._extract_receipt_serial), errors="coerce"
                )
            except Exception:
                g["_receipt_serial"] = pd.NA
            
            # 메인 먼저 정렬: 접수날짜 → 접수번호 → 원본순서 (원본 순위 무시)
            main_rows = g[g["_is_sub"] == 0].sort_values(
                ["_date_ord", "_receipt_serial", "_orig_order"],
                na_position="last", kind="mergesort"
            ).copy()
            main_rows["_new_base"] = range(1, len(main_rows) + 1)
            
            # 서브는 같은 기존 _rank_base 기준으로 메인 바로 뒤에 붙이되 날짜/원본순서 보존
            out_rows: list[dict] = []
            for _, main in main_rows.iterrows():
                base_val = main["_rank_base"]
                new_base = int(main["_new_base"])
                # 메인 푸시
                rec = dict(main)
                rec["_new_base"] = new_base
                rec["_sub_seq"] = None
                try:
                    main_jibeon = str(main.get("지번일련번호", "")).strip()
                    rec["_local_main_anchor"] = f"{main_jibeon}|{new_base}" if main_jibeon else str(new_base)
                except Exception:
                    rec["_local_main_anchor"] = str(new_base)
                out_rows.append(rec)
                # 서브 후보 풀: 순위/베이스와 무관하게 전체 서브에서 선택
                subs_all = g[(g["_is_sub"] == 1)].copy()
                own_key = str(main.get("_접수키", "")).strip()
                owner  = str(main.get("대상소유자", "")).replace("\u200B", "").strip()
                subs = pd.DataFrame(columns=g.columns)
                if _DBG_MATCH_KEYS:
                    try:
                        print(f"[MATCH DEBUG][GAP] main base={base_val} own_key={own_key} owner={owner}")
                        if not subs_all.empty:
                            preview = subs_all[[c for c in ["메인행_접수키", "메인행_대상소유자", "대상소유자", "접수정보"] if c in subs_all.columns]].head(5)
                            print(preview.to_string(index=False))
                    except Exception:
                        pass
                if own_key:
                    key_ok = subs_all.get("메인행_접수키", "").astype(str) == own_key if "메인행_접수키" in subs_all.columns else None
                    if key_ok is not None:
                        if owner:
                            if "메인행_대상소유자" in subs_all.columns:
                                owner_ok = subs_all["메인행_대상소유자"].astype(str).str.replace("\u200B", "").str.strip() == owner
                                subs = subs_all[key_ok & owner_ok].copy()
                            elif "대상소유자" in subs_all.columns:
                                owner_ok = subs_all["대상소유자"].astype(str).str.replace("\u200B", "").str.strip() == owner
                                subs = subs_all[key_ok & owner_ok].copy()
                            else:
                                subs = subs_all[key_ok].copy()
                        else:
                            subs = subs_all[key_ok].copy()
                if _DBG_MATCH_KEYS:
                    try:
                        print("[MATCH DEBUG][GAP] sub filter:")
                        cols = [c for c in ["메인행_접수키", "메인행_대상소유자", "대상소유자", "접수정보"] if c in subs_all.columns]
                        if cols:
                            print(subs_all[cols].head(10).to_string(index=False))
                    except Exception:
                        pass
                if not subs.empty:
                    subs = subs.sort_values(["_date_ord", "_orig_order"], na_position="last")
                # 부여
                seq = 1
                for _, sub in subs.iterrows():
                    sub_rec = dict(sub)
                    sub_rec["_new_base"] = new_base
                    sub_rec["_sub_seq"] = seq
                    try:
                        sub_rec["_local_attach_anchor"] = rec.get("_local_main_anchor", "")
                    except Exception:
                        pass
                    seq += 1
                    out_rows.append(sub_rec)
                    if _DBG_MATCH_KEYS:
                        try:
                            _ak = str(sub.get("메인행_접수키", ""))
                            _ao = str(sub.get("메인행_대상소유자", "")) or str(sub.get("대상소유자", ""))
                            print(f"[MATCH DEBUG][GAP] attach sub_idx={getattr(sub, 'name', '-') } key={_ak} owner={_ao}")
                        except Exception:
                            pass

            g2 = pd.DataFrame(out_rows)
            def _format_row(row) -> str:
                base = int(row.get("_new_base", 0)) if pd.notna(row.get("_new_base", None)) else 0
                if pd.notna(row.get("_sub_seq", None)) and str(row.get("_sub_seq")) not in ("", "None"):
                    sub = int(row["_sub_seq"]) 
                else:
                    sub = None
                return f"{base}-{sub}" if sub is not None else str(base)
            g2["순위번호"] = g2.apply(_format_row, axis=1).astype(str)
            return g2

        # (요구사항 1) 가압류 포함 → 가압류 그룹, 그 외 압류, 나머지 3그룹으로 나눠 그룹별 재매김
        def _gap_cat(purpose: str) -> str:
            s = str(purpose or "")
            s_norm = re.sub(r"\s+", "", s)
            # 가압류 우선 분류
            if "가압류" in s_norm:
                return "ga"
            # '압류' 단독만 분류(바로 앞 글자가 '가'인 경우는 제외)
            if re.search(r"(?<!가)압류", s_norm):
                return "ap"
            return "etc"
        out["_gap_cat"] = out.get("등기목적", "").apply(_gap_cat)

        ordered = []
        for key in ["ga", "ap", "etc"]:
            g = out[out["_gap_cat"] == key].copy()
            if g.empty:
                continue
            g = _renumber_one_group(g)
            ordered.append(g)
        # FutureWarning 방지: 빈/전부-NA 프레임 제외 후 concat
        ordered = [df for df in ordered if df is not None and not df.empty and df.count().sum() > 0]
        out = pd.concat(ordered, ignore_index=True) if ordered else out

        # [DEBUG] 재매김 직후 특정 키 카운트 확인
        try:
            import os as _os
            _dbg_item = _os.environ.get("DBG_GAP_ITEM", "R-019_01")
            _dbg_key = _os.environ.get("DBG_GAP_KEY", "2025-06-05|2815917")
            if {"물건번호", "_접수키"}.issubset(out.columns):
                _m = (out["물건번호"].astype(str) == str(_dbg_item)) & (out["_접수키"].astype(str) == str(_dbg_key))
                pass
        except Exception:
            pass

        # 순위번호 동률 시 접수날짜가 빠른 행 우선 (로컬 재매김 결과 활용)
        try:
            def _parse_new_rank(s: str) -> tuple[float, float]:
                s2 = str(s or "").replace("\u200B", "").strip()
                m = re.match(r"^(\d+)(?:-(\d+))?$", s2)
                if not m:
                    return float('inf'), float('inf')
                base = float(m.group(1))
                sub = float(m.group(2)) if m.group(2) else float('inf')
                return base, sub
            pr = out.get("순위번호", "").apply(_parse_new_rank)
            out["_nr_base"] = pr.apply(lambda x: x[0])
            out["_nr_sub"] = pr.apply(lambda x: x[1])
            out["_dt2"] = pd.to_datetime(out.get("접수날짜", ""), errors="coerce")
            out = out.sort_values(["_nr_base", "_nr_sub", "_dt2"], na_position="last")
        except Exception:
            pass

        # 순위번호 동률 시 접수날짜가 빠른 행 우선
        try:
            def _parse_new_rank(s: str) -> tuple[float, float]:
                # 제로폭 공백 제거 후 파싱
                s2 = str(s or "").replace("\u200B", "").strip()
                m = re.match(r"^(\d+)(?:-(\d+))?$", s2)
                if not m:
                    return float('inf'), float('inf')
                base = float(m.group(1))
                sub = float(m.group(2)) if m.group(2) else float('inf')
                return base, sub
            pr = out.get("순위번호", "").apply(_parse_new_rank)
            out["_nr_base"] = pr.apply(lambda x: x[0])
            out["_nr_sub"] = pr.apply(lambda x: x[1])
            out["_dt2"] = pd.to_datetime(out.get("접수날짜", ""), errors="coerce")
            out["_cat_ord"] = out.get("_gap_cat", "").map({"ga": 0, "ap": 1, "etc": 2})
            out = out.sort_values(["_cat_ord", "_nr_base", "_nr_sub", "_dt2"], na_position="last")
        except Exception:
            pass

        # Excel 날짜 자동변환 방지: 하이픈 포함 순위번호에 제로폭공백 접두
        try:
            mask_h = out["순위번호"].astype(str).str.contains("-", na=False)
            out.loc[mask_h, "순위번호"] = "\u200B" + out.loc[mask_h, "순위번호"].astype(str)
        except Exception:
            pass

        # 보조 컬럼 제거 및 최종 출력 정리
        out = out.drop(columns=[
            "_rank_base", "_rank_sub", "_is_sub", "_date_ord", "_new_base", "_sub_seq", "_접수날짜_dt", "_rank_base_only", "_serial_int", "_gap_cat", "_nr_base", "_nr_sub", "_dt2", "_cat_ord"
        ], errors="ignore")

        # [DEBUG] 최종 반환 직전 특정 키 카운트 확인
        try:
            import os as _os
            _dbg_item = _os.environ.get("DBG_GAP_ITEM", "R-019_01")
            _dbg_key = _os.environ.get("DBG_GAP_KEY", "2025-06-05|2815917")
            if {"물건번호", "_접수키"}.issubset(out.columns):
                _m = (out["물건번호"].astype(str) == str(_dbg_item)) & (out["_접수키"].astype(str) == str(_dbg_key))
                pass
        except Exception:
            pass

        # 지번번호는 병합 그룹에서 실제 등장한 지번일련번호만으로 구성해야 하므로
        # '지번번호들' 폴백 덮어쓰기를 제거한다 (오입력 방지)
        if "지번번호들" in out.columns:
            out = out.drop(columns=["지번번호들"], errors="ignore")
        # 최종 컬럼 순서로 정렬
        out = out.reindex(columns=self.GAP_PARSED_COLS)
        
        return out

    def parse_eul(self, df: pd.DataFrame) -> pd.DataFrame:
        """을구 테이블을 파싱하여 최종 컬럼 형태로 변환"""
        if df is None or df.empty:
            return pd.DataFrame(columns=self.EUL_PARSED_COLS)
        
        out = df.copy()
        
        # 중복된 "주요등기사항" 컬럼들을 모두 합침 (주요등기사항, 주요등기사항.1, 주요등기사항.2 등)
        # 이렇게 하면 두 컬럼으로 나뉜 데이터를 모두 하나의 문자열로 합칠 수 있습니다
        major_cols = [c for c in out.columns if c == "주요등기사항" or c.startswith("주요등기사항.")]
        
        if major_cols:
            def _combine_major_cols(row):
                parts = []
                for col in major_cols:
                    val = str(row.get(col, "")).strip()
                    if val and val.lower() not in ("", "nan", "none"):
                        parts.append(val)
                return " ".join(parts)
            out["_combined_주요등기사항"] = out.apply(_combine_major_cols, axis=1)
        else:
            out["_combined_주요등기사항"] = out.get("주요등기사항", "").astype(str)
        
        # 기존 컬럼 유지 및 새 컬럼 추가
        # 접수정보는 원본 그대로 유지
        out["접수정보"] = out.get("접수정보", "").astype(str)

        # 같은 지번일련번호에서 들어온 행은 병합하지 않기 위한 출처 지번 보존
        try:
            def _first_jibeon_eul(v):
                try:
                    if isinstance(v, (list, tuple)):
                        return str(v[0]).strip() if v else ""
                    return str(v).strip()
                except Exception:
                    return str(v)
            if "지번일련번호" in out.columns:
                out["_src_jibeon"] = out["지번일련번호"].apply(_first_jibeon_eul)
            else:
                out["_src_jibeon"] = ""
        except Exception:
            out["_src_jibeon"] = ""

        # 순위번호 정규화: "1 (전 8)" → "1"
        if "순위번호" in out.columns:
            out["순위번호"] = out["순위번호"].astype(str).map(self._normalize_rank_with_prev)
        
        # 접수날짜는 접수정보에서 추출 (YYYY-MM-DD 형태)
        out["접수날짜"] = out["접수정보"].apply(self._extract_receipt_date)
        # 접수번호는 접수정보 기반으로 생성
        out["접수번호"] = out["접수정보"].apply(self._build_receipt_key)
        # 합쳐진 주요등기사항에서 권리자/금액(임차 포함) 추출
        out["근저당권자/전세권자/채권자/지상권자/임차권자"] = out["_combined_주요등기사항"].apply(self._extract_creditor)
        out["채권최고액/전세금/임차보증금"] = out["_combined_주요등기사항"].apply(self._extract_amount)
        # 표시용 쉼표 제거(문자열 유지)
        try:
            if "채권최고액/전세금/임차보증금" in out.columns:
                out["채권최고액/전세금/임차보증금"] = out["채권최고액/전세금/임차보증금"].astype(str).str.replace(",", "", regex=False)
        except Exception:
            pass
        
        # 폴백: 금액/권리자가 비어있을 때, 합쳐진 주요등기사항이나 다른 열에서 재추출
        try:
            # 권리자 폴백
            mask_owner_empty = out["근저당권자/전세권자/채권자/지상권자/임차권자"].astype(str).str.strip() == ""
            if mask_owner_empty.any():
                out.loc[mask_owner_empty, "근저당권자/전세권자/채권자/지상권자/임차권자"] = (
                    out.loc[mask_owner_empty, "_combined_주요등기사항"].apply(self._extract_creditor)
                )
            # 금액 폴백
            mask_amt_empty = out["채권최고액/전세금/임차보증금"].astype(str).str.strip() == ""
            if mask_amt_empty.any():
                out.loc[mask_amt_empty, "채권최고액/전세금/임차보증금"] = (
                    out.loc[mask_amt_empty, "_combined_주요등기사항"].apply(self._extract_amount)
                )
        except Exception:
            pass
        
        # 권리자/임차권자 컬럼의 띄어쓰기 제거 (병합 전)
        out["근저당권자/전세권자/채권자/지상권자/임차권자"] = out["근저당권자/전세권자/채권자/지상권자/임차권자"].astype(str).str.replace(r"\s+", "", regex=True)
        
        # 원본 주요등기사항 컬럼을 합쳐진 버전으로 업데이트 (나중에 출력용)
        out["주요등기사항"] = out["_combined_주요등기사항"]

        # 원본 보관(라벨/접수일 상이 계산용)
        eul_raw_for_labels = out.copy()

        # 1) 목적 통합 그룹 정의 (근저당권설정 + 전세권설정 + 지상권설정)
        def _purpose_group(purpose: str) -> str:
            p = str(purpose).strip()
            if p in ["근저당권설정", "전세권설정", "지상권설정"]:
                return "담보권설정"
            return "기타"
        out["_purpose_group"] = out.get("등기목적", "").apply(_purpose_group)
        out["_purpose_weight"] = out["_purpose_group"].map({"담보권설정": 0, "기타": 1})

        # 2) 순위번호 파싱 (기본-서브 분리)
        def _parse_rank(rank_str: str) -> tuple[float, float]:
            try:
                s = str(rank_str).strip()
                if not s:
                    return float('inf'), float('inf')
                m = re.match(r"^(\d+)(?:-(\d+))?$", s)
                if not m:
                    return float('inf'), float('inf')
                base = float(m.group(1))
                sub = float(m.group(2)) if m.group(2) else float('inf')
                return base, sub
            except Exception:
                return float('inf'), float('inf')
        
        rank_parsed = out.get("순위번호", "").apply(_parse_rank)
        out["_rank_base"] = rank_parsed.apply(lambda x: x[0])
        out["_rank_sub"] = rank_parsed.apply(lambda x: x[1])
        out["_is_sub"] = (out["_rank_sub"] != float('inf')).astype(int)
        
        # 원본 행 순서 보존 (OCR 테이블 순서)
        out["_orig_order"] = range(len(out))
        
        # DEBUG 제거: 순위번호 파싱 결과 출력 제거
        
        # 서브행에 메인행의 접수정보 저장 (원본 순서 기준으로 바로 위 메인행 찾기)
        out["메인행_접수정보"] = ""
        out["메인행_접수번호"] = ""
        out["메인행_접수키"] = ""
        # 메인행 대상소유자 앵커도 저장해 이후 매칭에 활용
        out["메인행_대상소유자"] = ""
        
        for idx in out.index:
            if out.loc[idx, "_is_sub"] == 1:  # 서브행인 경우
                sub_base = out.loc[idx, "_rank_base"]
                sub_order = out.loc[idx, "_orig_order"]
                sub_owner = str(out.loc[idx, "대상소유자"]).strip()
                
                # 현재 서브행보다 앞에 있고, 같은 _rank_base를 가진 메인행들 중에서
                candidates = out[
                    (out["_is_sub"] == 0) & 
                    (out["_rank_base"] == sub_base) & 
                    (out["_orig_order"] < sub_order)
                ]
                
                if not candidates.empty:
                    # 대상소유자가 일치하는 메인행을 우선 찾기
                    owner_match = candidates[candidates["대상소유자"].astype(str).str.strip() == sub_owner]
                    
                    if not owner_match.empty:
                        # 대상소유자가 일치하는 메인행 중 가장 가까운 것
                        main_idx = owner_match.loc[owner_match["_orig_order"].idxmax()]
                    else:
                        # 대상소유자 매칭 실패 시 순서상 가장 가까운 메인행
                        main_idx = candidates.loc[candidates["_orig_order"].idxmax()]
                    
                    out.loc[idx, "메인행_접수정보"] = str(main_idx["접수정보"])
                    # 통일된 접수 키(날짜|일련)로 저장하여 후속 매핑 키와 형식 일치
                    try:
                        _mk = TableProcessor._build_receipt_key(main_idx.get("접수정보", ""))
                    except Exception:
                        _mk = str(main_idx.get("접수정보", ""))
                    out.loc[idx, "메인행_접수번호"] = _mk
                    out.loc[idx, "메인행_접수키"] = _mk
                    try:
                        out.loc[idx, "메인행_대상소유자"] = str(main_idx.get("대상소유자", "")).replace("\u200B", "").strip()
                    except Exception:
                        pass
        
        # DEBUG 제거: 서브행에 메인행 접수정보 매핑 출력 제거

        # 3) 접수날짜 정렬용 보조 컬럼
        out["_date_ord"] = pd.to_datetime(out.get("접수날짜", ""), errors="coerce")
        # 매칭 키 디버그 출력 플래그
        try:
            import os as _os
            def _env_true(name: str) -> bool:
                v = str(_os.environ.get(name, "0")).strip().lower()
                return v in ("1", "true", "yes", "on")
            _DBG_MATCH_KEYS = _env_true("DBG_MATCH_KEYS")
        except Exception:
            _DBG_MATCH_KEYS = False

        # 4) 병합 전 정렬: 목적 가중치 → 접수날짜 (순위 기반 정렬 제거)
        out = out.sort_values([
            "_purpose_weight", "_date_ord"
        ], na_position="last").reset_index(drop=True)

        # 접수번호 기준 병합: 같은 (물건번호, 등기목적, 접수번호) 묶음으로 통합하고 순위번호는 최소 선택
        def _first_non_empty(series: pd.Series) -> str:
            for v in series.astype(str).tolist():
                s = str(v).strip()
                if s:
                    return s
            return ""
        # 표준 임시 병합키 생성: _권리자, _금액 (을구)
        if "근저당권자/전세권자/채권자/지상권자/임차권자" in out.columns:
            out["_권리자"] = out["근저당권자/전세권자/채권자/지상권자/임차권자"].astype(str).str.replace(r"\s+", "", regex=True)
        if "채권최고액/전세금/임차보증금" in out.columns:
            out["_금액"] = out["채권최고액/전세금/임차보증금"].astype(str).map(lambda x: re.sub(r"[^0-9]", "", x))

        # 병합용 접수번호: 각 행 고유의 접수번호 사용 (서브행도 자체 접수 유지)
        out["_merge_receipt"] = out["접수번호"].astype(str)
        # 대상소유자 정규화 키 준비(공백 제거)
        try:
            out["_owner_key"] = out.get("대상소유자", "").astype(str).str.replace(r"\s+", "", regex=True)
        except Exception:
            out["_owner_key"] = out.get("대상소유자", "")
        
        # DEBUG 제거: 병합용 접수번호 설정 출력 제거

        # 병합 키(요구사항): 접수번호 + 대상소유자(정규화)
        merge_keys = [c for c in ["물건번호", "_merge_receipt", "_owner_key"] if c in out.columns]
        if merge_keys:
            # 집계 전에 후보 메인 접수번호 리스트 컬럼을 준비(존재하지 않으면 생성)
            try:
                if "메인행_접수번호" in out.columns and "메인행_접수번호들" not in out.columns:
                    out["메인행_접수번호들"] = out["메인행_접수번호"].astype(str)
            except Exception:
                pass
            # 디버그 출력 제거
            
            agg_map: dict[str, any] = {}
            # 집계 시 가장 이른 접수정보/접수날짜를 선택하도록 보조 함수 정의
            def _pick_earliest_receipt(series: pd.Series) -> str:
                best_tuple = None
                best_text = ""
                for v in series.astype(str).tolist():
                    s = str(v).strip()
                    if not s:
                        continue
                    d = TableProcessor._extract_date(s)
                    serial = TableProcessor._extract_receipt_serial(s)
                    key = (
                        pd.to_datetime(d, errors="coerce"),
                        pd.to_numeric(serial, errors="coerce")
                    )
                    if best_tuple is None or (
                        (pd.notna(key[0]) and (pd.isna(best_tuple[0]) or key[0] < best_tuple[0])) or
                        (pd.isna(key[0]) and pd.isna(best_tuple[0]) and pd.notna(key[1]) and (pd.isna(best_tuple[1]) or key[1] < best_tuple[1]))
                    ):
                        best_tuple = key
                        best_text = s
                return best_text

            def _min_date_str(series: pd.Series) -> str:
                dt = pd.to_datetime(series.astype(str), errors="coerce")
                if dt.notna().any():
                    return str(dt.min().date())
                return ""

            if "순위번호" in out.columns:
                # 메인/서브 텍스트를 그대로 유지(가장 작은 값을 고르는 대신 대표 텍스트 선택)
                def _pick_best_rank_text(series: pd.Series) -> str:
                    best = None
                    for v in series.astype(str).tolist():
                        s = str(v).strip()
                        if not s:
                            continue
                        # 우선순위: 숫자 → 숫자-서브 → 기타
                        m = re.match(r"^(\d+)(?:-(\d+))?$", s)
                        if m:
                            base = int(m.group(1))
                            sub = int(m.group(2)) if m.group(2) else 0
                            key = (0, base, sub)
                        else:
                            key = (1, 10**9, 10**9)
                        if best is None or key < best[0]:
                            best = (key, s)
                    return best[1] if best else ""
                agg_map["순위번호"] = _pick_best_rank_text
            # 순위번호 관련 보조 컬럼: 병합 후 재배치용 힌트만 유지
            if "_rank_base" in out.columns:
                agg_map["_rank_base"] = lambda s: s.min()
            if "_rank_sub" in out.columns:
                agg_map["_rank_sub"] = lambda s: s.min()
            if "_is_sub" in out.columns:
                agg_map["_is_sub"] = lambda s: s.min()
            if "메인행_접수정보" in out.columns:
                agg_map["메인행_접수정보"] = _first_non_empty
            if "메인행_접수키" in out.columns:
                agg_map["메인행_접수키"] = _first_non_empty
            # 메인행 접수번호는 모든 후보를 보존해 최종 단계에서 재선택할 수 있도록 리스트로도 보관
            def _collect_receipts(s: pd.Series) -> str:
                vals = [str(v).strip() for v in s.astype(str).tolist() if str(v).strip()]
                uniq = sorted(dict.fromkeys(vals))
                return ",".join(uniq)
            if "_purpose_weight" in out.columns:
                agg_map["_purpose_weight"] = lambda s: s.min()
            if "_date_ord" in out.columns:
                agg_map["_date_ord"] = lambda s: s.min()
            # 접수정보/접수날짜는 가장 이른 값으로 집계
            if "접수정보" in out.columns:
                agg_map["접수정보"] = _pick_earliest_receipt
            if "접수날짜" in out.columns:
                agg_map["접수날짜"] = _min_date_str
            # 접수번호 리스트 보관 (서브행 매칭용)
            if "접수번호" in out.columns:
                agg_map["접수번호"] = lambda s: ",".join(sorted(set(str(x) for x in s.astype(str).tolist() if str(x).strip())))
            # 메인행 접수정보/번호도 보관
            if "메인행_접수정보" in out.columns:
                agg_map["메인행_접수정보"] = _first_non_empty
            if "메인행_접수번호" in out.columns:
                agg_map["메인행_접수번호"] = _first_non_empty
                agg_map["메인행_접수번호들"] = _collect_receipts
            if "_orig_order" in out.columns:
                agg_map["_orig_order"] = lambda s: s.min()
            mk_set = set(merge_keys)
            for c in ["등기목적", "근저당권자/전세권자/채권자/지상권자/임차권자", "채권최고액/전세금/임차보증금", "채무자", "담보종류", "공장저당", "주요등기사항", "대상소유자", "_권리자", "_금액"]:
                if c in out.columns and c not in mk_set:
                    agg_map[c] = _first_non_empty
            if "지번일련번호" in out.columns:
                def _collect_ids(s: pd.Series) -> list[str]:
                    vals = [str(v).strip() for v in s.astype(str).tolist() if str(v).strip()]
                    ids: set[str] = set()
                    for v in vals:
                        for part in str(v).split(','):
                            p = part.strip()
                            if p:
                                ids.add(p)
                    return sorted(ids)
                agg_map["지번일련번호"] = _collect_ids
            # DEBUG 제거: 병합 전 데이터 확인 출력 제거
            
            
            out = (
                out.groupby(merge_keys, dropna=False)
                   .agg(agg_map)
                   .reset_index()
            )
            
            
            # 임시 표준 병합키는 이후 단계에서도 사용할 수 있으므로 유지

        # 원본 기준 라벨/접수일 상이 계산 후 조인 (을구)
        try:
            def _norm_owner_any(s):
                return re.sub(r"\s+", "", str(s))
            def _norm_amt_any(s):
                try:
                    v = pd.to_numeric(s, errors="coerce")
                    if pd.notna(v):
                        return str(int(v))
                except Exception:
                    pass
                return re.sub(r"[^0-9]", "", str(s))

            eul_raw_for_labels["_k_등기목적"] = eul_raw_for_labels.get("등기목적", "").astype(str)
            eul_raw_for_labels["_k_권리자"] = eul_raw_for_labels.get("근저당권자/전세권자/채권자/지상권자/임차권자", "").map(_norm_owner_any)
            eul_raw_for_labels["_k_금액"] = eul_raw_for_labels.get("채권최고액/전세금/임차보증금", "").map(_norm_amt_any)
            eul_raw_for_labels["_k_대상소유자"] = eul_raw_for_labels.get("대상소유자", "").map(_norm_owner_any)

            lbl_keys_eul = [c for c in ["물건번호", "_k_등기목적", "_k_권리자", "_k_금액", "_k_대상소유자"] if c in eul_raw_for_labels.columns]


            def _make_labels_eul(g: pd.DataFrame) -> pd.Series:
                pairs = []
                for _, r in g.iterrows():
                    ids = str(r.get("지번일련번호", ""))
                    date = pd.to_datetime(str(r.get("접수날짜", "")).strip(), errors="coerce")
                    for v in ids.split(',') if ids else []:
                        vid = v.strip()
                        if vid:
                            pairs.append((vid, date))
                if not pairs:
                    return pd.Series({"지번번호": "", "접수일 상이": ""})
                def _suf(x: str) -> str:
                    last = re.split(r"[_-]", x)[-1]
                    return last.zfill(2) if last.isdigit() else last
                id_to_date = {}
                for vid, d in pairs:
                    if vid not in id_to_date or (pd.notna(d) and (pd.isna(id_to_date[vid]) or d < id_to_date[vid])):
                        id_to_date[vid] = d
                valid_dates = [d for d in id_to_date.values() if pd.notna(d)]
                earliest_date = min(valid_dates) if valid_dates else pd.NaT
                labels, mism = [], []
                for vid, d in id_to_date.items():
                    suf = _suf(vid)
                    lab = "'" + suf if suf.isdigit() else suf
                    labels.append(lab)
                    if pd.notna(earliest_date) and (pd.isna(d) or d != earliest_date):
                        if suf not in mism:
                            mism.append(suf)
                def _ord(lbl: str) -> int:
                    m = re.match(r"^'?([0-9]{2})", lbl)
                    return int(m.group(1)) if m else 10**9
                labels = sorted(dict.fromkeys(labels), key=_ord)
                mism = sorted(dict.fromkeys(mism))
                return pd.Series({"지번번호": ", ".join(labels), "접수일 상이": ",".join(mism)})

            geo_eul = (
                eul_raw_for_labels.groupby(lbl_keys_eul, dropna=False)
                    .apply(_make_labels_eul, include_groups=False)
                    .reset_index()
            )
            # out 쪽 정규화 키 준비 및 조인
            out["_k_등기목적"] = out.get("등기목적", "").astype(str)
            # out에는 집계 전 생성한 임시 키(_권리자, _금액)가 남아있으므로 이를 사용
            out["_k_권리자"] = out.get("_권리자", "").map(_norm_owner_any) if "_권리자" in out.columns else ""
            out["_k_금액"] = out.get("_금액", "").map(_norm_amt_any) if "_금액" in out.columns else ""
            out["_k_대상소유자"] = out.get("대상소유자", "").map(_norm_owner_any) if "대상소유자" in out.columns else ""
            out = out.merge(geo_eul, on=lbl_keys_eul, how="left")
            out = out.drop(columns=[c for c in ["_k_등기목적","_k_권리자","_k_금액","_k_대상소유자","_권리자","_금액"] if c in out.columns], errors="ignore")
        except Exception:
            pass

        
        # 빈 컬럼 추가 (을구용)
        out["채무자"] = ""
        out["담보종류"] = ""
        out["공장저당"] = ""
        
        # 비고 컬럼 추가: 가처분의 피보전권리, 지상권설정의 "건물 기타 공작물이나 수목의 소유"
        def _extract_pibojun_content(major_content: str, purpose: str) -> str:
            """가처분 등기목적의 경우 피보전권리 뒤의 내용을 추출"""
            if str(purpose).strip() != "가처분":
                return ""
            
            text = str(major_content).strip()
            if not text:
                return ""
            
            # "피보전권리" 뒤의 내용을 추출하는 정규식
            pattern = r"피보전권리\s*([^.]*?)(?:\s*채권자|$)"
            match = re.search(pattern, text)
            if match:
                content = match.group(1).strip()
                # 불필요한 공백 제거
                content = re.sub(r'\s+', ' ', content).strip()
                return content
            
            return ""

        def _extract_easement_note(major_content: str, purpose: str) -> str:
            """지상권설정의 경우 '목적' 라벨 뒤 텍스트를 다음 라벨 전까지 추출"""
            if str(purpose).strip() != "지상권설정":
                return ""
            text = str(major_content or "")
            if not text:
                return ""
            # '목 적' / '목적' 모두 허용, 다음 라벨(지상권자/근저당권자/전세권자/채권자/임차권자/담보종류/공장저당/채무자) 전까지
            pattern = r"목\s*적\s*([\s\S]*?)(?=\s*(?:지상권자|근저당권자|전세권자|채권자|임차권자|담보종류|공장저당|채무자|$))"
            m = re.search(pattern, text, flags=re.IGNORECASE)
            if m:
                content = m.group(1)
                content = re.sub(r"\s+", " ", content).strip()
                return content
            return ""

        def _extract_mortgage_correction_note(major_content: str, purpose: str) -> str:
            """근저당권경정의 경우 '목적' 뒤 텍스트를 다음 라벨 전까지 추출"""
            if str(purpose).strip() != "근저당권경정":
                return ""
            text = str(major_content or "")
            if not text:
                return ""
            pattern = r"목\s*적\s*([\s\S]*?)(?=\s*(?:근저당권자|전세권자|지상권자|채권자|임차권자|담보종류|공장저당|채무자|$))"
            m = re.search(pattern, text, flags=re.IGNORECASE)
            if m:
                content = m.group(1)
                content = re.sub(r"\s+", " ", content).strip()
                return content
            return ""

        def _build_remark(row: pd.Series) -> str:
            major = row.get("주요등기사항", "")
            purpose = row.get("등기목적", "")
            parts = []
            p1 = _extract_pibojun_content(major, purpose)
            if p1:
                parts.append(p1)
            p2 = _extract_easement_note(major, purpose)
            if p2:
                parts.append(p2)
            p3 = _extract_mortgage_correction_note(major, purpose)
            if p3:
                parts.append(p3)
            return "; ".join(parts)

        out["비고"] = out.apply(_build_remark, axis=1)
        # 지번번호 = 지번일련번호의 마지막 세그먼트(R-001_01_01 -> 01)
        def _suffix_from_jibeon(val: str) -> str:
            try:
                s = str(val).strip()
                if not s:
                    return ""
                parts = re.split(r"[_-]", s)
                return parts[-1] if parts else ""
            except Exception:
                return ""
        # 1안 구현을 위해 여기서는 지번번호를 아직 만들지 않고, 지번일련번호 컬럼을 보존
        if "지번일련번호" not in out.columns:
            out["지번번호"] = ""
        
        # 대상소유자 컬럼이 없으면 빈 값으로 채우기
        if "대상소유자" not in out.columns:
            out["대상소유자"] = ""
        
        # 내용 기준 축약 로직은 제거 (접수정보 기준 병합만 유지)
        content_cols = []
        if False and content_cols and "지번일련번호" in out.columns:
            # 그룹 키 표준화: NaN/빈칸/공백 차이로 다른 그룹이 나뉘지 않도록 문자열 정규화
            tmp_for_group = out.copy()
            for cc in content_cols:
                if cc in tmp_for_group.columns:
                    # NaN → "" 후 문자열화, 공백 제거, 'nan' 문자열도 공백으로 통일
                    col = tmp_for_group[cc].fillna("")
                    col = col.astype(str).str.strip()
                    col = col.replace({"nan": "", "None": ""})
                    # 주요등기사항은 띄어쓰기도 제거 (병합 기준 - 출력용 원본은 유지)
                    if cc == "주요등기사항":
                        col = col.str.replace(r"\s+", "", regex=True)
                    tmp_for_group[cc] = col
            def _suffix(v: str) -> str:
                last = re.split(r"[_-]", str(v).strip())[-1]
                return last.zfill(2) if last.isdigit() else last

            merged_records: list[dict] = []
            for _, g in tmp_for_group.groupby(content_cols, dropna=False):
                # 실제 데이터 접근은 원본 out에서 index로 조회
                g = out.loc[g.index]
                # 동일 내용 그룹: 같은 지번일련번호 내에서 들어온 행은 절대 축약하지 않음
                # _src_jibeon이 없으면 지번일련번호의 suffix로 근사 판단
                unique_src = set()
                if "_src_jibeon" in g.columns:
                    unique_src = set(str(x).strip() for x in g["_src_jibeon"].tolist())
                else:
                    def _suffix_only(v: str) -> str:
                        last = re.split(r"[_-]", str(v).strip())[-1]
                        return last.zfill(2) if last.isdigit() else last
                    for vv in g.get("지번일련번호", "").astype(str).tolist():
                        if not vv:
                            continue
                        unique_src.add(_suffix_only(vv))
                if len(g) <= 1 or len(unique_src) <= 1:
                    merged_records.extend(g.to_dict(orient="records"))
                    continue

                infos = []  # (row_idx, suffix, rec_info, rec_date)
                for ridx, r in g.iterrows():
                    v = r.get("지번일련번호", "")
                    ids = v if isinstance(v, list) else [v]
                    rec_info = str(r.get("접수정보", "")).strip()
                    rec_date = pd.to_datetime(str(r.get("접수날짜", "")).strip(), errors="coerce")
                    for one in ids:
                        suf = _suffix(one)
                        if suf:
                            infos.append((ridx, suf, rec_info, rec_date))

                # 대표 기준: 가장 이른 접수날짜가 있는 행, 없으면 첫 행
                valid = [(ridx, suf, info, d) for (ridx, suf, info, d) in infos if pd.notna(d)]
                if valid:
                    ridx_rep, suf_rep, info_rep, date_rep = sorted(valid, key=lambda x: x[3])[0]
                    rep = g.loc[ridx_rep].copy()
                    rep["접수정보"] = info_rep
                    rep["접수날짜"] = date_rep.strftime("%Y-%m-%d")
                else:
                    ridx_rep, suf_rep, info_rep, date_rep = infos[0] if infos else (g.index[0], "", str(g.iloc[0].get("접수정보", "")), pd.NaT)
                    rep = g.loc[ridx_rep].copy()

                # 지번번호 라벨 구성: 전체 지번의 suffix를 라벨로 정리
                all_labels = []
                uniq_suffixes = set()
                for (_, suf, _, _) in infos:
                    if suf:
                        uniq_suffixes.add(suf)
                        all_labels.append(f"'{suf}")
                # 정렬 및 중복 제거
                def _order_key(lbl: str) -> int:
                    m = re.match(r"^'?(\d{2})", lbl)
                    return int(m.group(1)) if m else 10**9
                all_labels = sorted(dict.fromkeys(all_labels), key=_order_key)
                rep["지번번호"] = ", ".join(all_labels)

                # 접수일 상이 관련 표기 제거
                if "접수일 상이" in rep.index:
                    rep["접수일 상이"] = ""

                # 대표 행의 지번일련번호는 산출물에서 비워둠(라벨로 대체)
                if "지번일련번호" in rep.index:
                    rep["지번일련번호"] = ""
                merged_records.append(rep.to_dict())

            out = pd.DataFrame(merged_records)


        # 5) 최종 정렬: 접수날짜 → 접수번호 (로컬순위번호 제외, 서브 배치 로직은 유지)
        # 접수번호 일련값
        try:
            out["_receipt_serial"] = pd.to_numeric(out.get("접수정보", "").astype(str).map(TableProcessor._extract_receipt_serial), errors="coerce")
        except Exception:
            out["_receipt_serial"] = pd.NA
        
        # 접수날짜/접수번호 기준으로 정렬 (메인/서브 구분은 이후 최종 정렬에서 보장)
        out = out.sort_values(["_date_ord", "_receipt_serial", "_orig_order"], na_position="last", kind="mergesort").reset_index(drop=True)
        

        # 6) 최종 재매김: 최종 정렬 결과에 따라 메인은 1..N, 서브는 메인별 1..M
        try:
            # 메인 행에 최종 base 부여
            out["_final_base"] = pd.NA
            main_mask = (out.get("_is_sub", 0) == 0)
            main_idx = out.index[main_mask]
            out.loc[main_idx, "_final_base"] = range(1, main_mask.sum() + 1)

            # 메인 (접수키, 대상소유자) → base 매핑 (동시일치용)
            def _build_key(val: str) -> str:
                try:
                    return TableProcessor._build_receipt_key(val)
                except Exception:
                    return str(val)
            def _norm_owner(val: str) -> str:
                return re.sub(r"\s+", "", str(val or "")).replace("\u200B", "").strip()

            rec_to_base: dict[str, int] = {}
            rec_owner_to_base: dict[tuple[str, str], int] = {}
            for i in main_idx:
                rec_key = _build_key(out.at[i, "접수정보"]) if "접수정보" in out.columns else ""
                owner_k = _norm_owner(out.at[i, "대상소유자"]) if "대상소유자" in out.columns else ""
                base_v = int(out.at[i, "_final_base"]) if pd.notna(out.at[i, "_final_base"]) else None
                if base_v is None:
                    continue
                if rec_key:
                    rec_to_base[rec_key] = base_v
                if rec_key and owner_k:
                    rec_owner_to_base[(rec_key, owner_k)] = base_v

            # 서브 행에 최종 base 주입: (메인행_접수번호들/메인행_접수번호/메인행_접수키, 메인행_대상소유자) 동시 일치
            sub_mask = (out.get("_is_sub", 0) == 1)
            if sub_mask.any():
                def _map_pair_from_row(row) -> float | int | None:
                    # owner anchor 우선: 메인행_대상소유자 → 없으면 대상소유자
                    owner_anchor = row.get("메인행_대상소유자", "") or row.get("대상소유자", "")
                    owner_norm = _norm_owner(owner_anchor)
                    # 후보 키 모음: 메인행_접수번호들(콤마), 메인행_접수번호, 메인행_접수키
                    cand_keys: list[str] = []
                    vlist = str(row.get("메인행_접수번호들", "")).strip()
                    if vlist:
                        cand_keys.extend([s.strip() for s in vlist.split(',') if s.strip()])
                    for c in ["메인행_접수번호", "메인행_접수키"]:
                        vv = str(row.get(c, "")).strip()
                        if vv:
                            cand_keys.append(vv)
                    # 동시 일치 시도
                    for kk in cand_keys:
                        k = _build_key(kk)
                        if owner_norm and (k, owner_norm) in rec_owner_to_base:
                            return rec_owner_to_base[(k, owner_norm)]
                    # 동시 일치 실패 시 부착하지 않음(요구사항)
                    return None

                out.loc[sub_mask, "_final_base"] = out.loc[sub_mask].apply(_map_pair_from_row, axis=1)
                # 폴백 제거: 앵커 없이는 부착하지 않음(미부착 유지)
                # if out.loc[sub_mask, "_final_base"].isna().any():
                #     last_base = None
                #     for i in out.index:
                #         if out.at[i, "_is_sub"] == 0:
                #             last_base = out.at[i, "_final_base"]
                #         else:
                #             if pd.isna(out.at[i, "_final_base"]):
                #                 out.at[i, "_final_base"] = last_base

            # 서브 순번 부여: 메인별로 날짜→접수번호→원본순서 (원본 표기 서브번호는 무시하고 1부터 재부여)
            out["_final_sub"] = pd.NA
            sub_mask = (out.get("_is_sub", 0) == 1)
            if sub_mask.any():
                tmp = out.loc[sub_mask].sort_values(["_final_base", "_date_ord", "_receipt_serial", "_orig_order"], na_position="last", kind="mergesort")
                seq = tmp.groupby("_final_base").cumcount() + 1
                out.loc[tmp.index, "_final_sub"] = seq.values

            # 최종 순위번호 텍스트 생성
            def _fmt_final(row) -> str:
                base = int(row["_final_base"]) if pd.notna(row["_final_base"]) else 0
                if int(row.get("_is_sub", 0)) == 1:
                    sub = int(row["_final_sub"]) if pd.notna(row.get("_final_sub", pd.NA)) else 1
                    return f"{base}-{sub}"
                return str(base)
            out["순위번호"] = out.apply(_fmt_final, axis=1).astype(str)

            
            # 최종 출력 정렬: 메인→서브 순서 보장
            out = out.sort_values(["_final_base", "_is_sub", "_date_ord", "_receipt_serial", "_orig_order"], na_position="last", kind="mergesort").reset_index(drop=True)
        except Exception:
            pass
        # Excel의 날짜 자동변환 방지를 위해 하이픈 포함 순위번호에는 제로폭공백(U+200B) 접두
        try:
            mask_hyphen = out["순위번호"].astype(str).str.contains("-", na=False)
            out.loc[mask_hyphen, "순위번호"] = "\u200B" + out.loc[mask_hyphen, "순위번호"].astype(str)
        except Exception:
            pass
        
        # 금액 표기 정규화: "미화" 접두 제거, 달러 표기는 공백 포함, 원 금액은 정수화
        def _normalize_amount_output(v):
            s = str(v or "").strip()
            if not s:
                return s
            s = re.sub(r"\s+", " ", s)
            # '미화' 접두 제거
            s = re.sub(r"^미화\s*", "", s)
            # '불'을 '달러'로 통일
            s = s.replace("불", "달러")
            if "달러" in s:
                # 숫자/한글 뒤 '달러' 표기 앞에 공백 보장
                s = re.sub(r"\s*달러$", " 달러", s)
                return s
            # 원 금액: 콤마/텍스트 제거 후 정수화
            digits = re.sub(r"[^0-9]", "", s)
            if digits:
                try:
                    return int(digits)
                except Exception:
                    return digits
            return s

        if "채권최고액/전세금/임차보증금" in out.columns:
            out["채권최고액/전세금/임차보증금"] = out["채권최고액/전세금/임차보증금"].apply(_normalize_amount_output)
        
        # [최종 보정] 을구: 등기목적이 '요역지지역권'이면 특정 컬럼을 비움(전 단계 병합/집계로 재유입 방지)
        try:
            if "등기목적" in out.columns:
                _p2 = out["등기목적"].astype(str).str.replace(r"\s+", "", regex=True)
                _msk_yoryeok2 = _p2.str.contains("요역지지역권", na=False)
                if _msk_yoryeok2.any():
                    for _c in [
                        "접수정보",
                        "접수번호",
                        "근저당권자/전세권자/채권자/지상권자/임차권자",
                        "채권최고액/전세금/임차보증금",
                        "채무자",
                    ]:
                        if _c in out.columns:
                            out.loc[_msk_yoryeok2, _c] = ""
        except Exception:
            pass

        # 지번번호는 병합 결과의 지번일련번호에서 항상 다시 계산해 일관성 보장
        try:
            if "지번일련번호" in out.columns:
                def _label_from_ids_eul(val) -> str:
                    ids: list[str] = []
                    if isinstance(val, (list, tuple)):
                        ids = [str(v).strip() for v in val if str(v).strip()]
                    else:
                        ids = [p.strip() for p in str(val).split(',') if p.strip()]
                    import re as _re
                    def _suf(v: str) -> str:
                        last = _re.split(r"[_-]", v)[-1]
                        return last.zfill(2) if last.isdigit() else last
                    labels = [f"'{_suf(v)}" for v in ids if v]
                    def _ord(lbl: str) -> int:
                        m = re.match(r"^'?([0-9]{2})", lbl)
                        return int(m.group(1)) if m else 10**9
                    return ", ".join(sorted(dict.fromkeys(labels), key=_ord))
                out["지번번호"] = out["지번일련번호"].apply(_label_from_ids_eul)
        except Exception:
            pass
        if "지번번호들" in out.columns:
            out = out.drop(columns=["지번번호들"], errors="ignore")
        # 7) 보조 컬럼 제거 및 최종 컬럼 순서로 정렬
        out = out.drop(columns=[
            "_purpose_group", "_purpose_weight", "_rank_base", "_rank_sub", "_is_sub", "_date_ord", "_new_base", "_sub_seq"
        ], errors="ignore")
        out = out.reindex(columns=self.EUL_PARSED_COLS)
        
        return out

    def find_header_row(self, df: pd.DataFrame, required_cols: list[str]) -> int:
        for ridx in df.index:
            row_vals = [self.normalize(v) for v in df.loc[ridx, :].tolist()]
            if all(any(req in (cell or '') for cell in row_vals) for req in required_cols):
                return ridx
        return -1

    def set_header(self, df: pd.DataFrame, header_row_idx: int) -> pd.DataFrame:
        header = df.loc[header_row_idx, :].astype(str).apply(self.normalize).tolist()
        body = df.loc[df.index > header_row_idx, :].copy()
        
        # 중복된 컬럼명에 번호를 붙여서 보존 (예: "주요등기사항", "주요등기사항.1", "주요등기사항.2")
        new_header = []
        col_counts = {}
        for col_name in header:
            if col_name in col_counts:
                col_counts[col_name] += 1
                new_header.append(f"{col_name}.{col_counts[col_name]}")
            else:
                col_counts[col_name] = 0
                new_header.append(col_name)
        
        body.columns = new_header
        return body.reset_index(drop=True)

    def build_df_from_cells(self, cells: list[dict]) -> pd.DataFrame:
        table_dict: dict[int, dict[int, str]] = {}
        for cell in cells:
            r = int(cell.get('rowIndex', 0))
            c = int(cell.get('columnIndex', 0))
            cell_text_lines = cell.get('cellTextLines', [])
            text = ' '.join(
                word.get('inferText', '')
                for line in cell_text_lines
                for word in line.get('cellWords', [])
            )
            table_dict.setdefault(r, {})[c] = self.normalize(text)
        if not table_dict:
            return pd.DataFrame()
        df = pd.DataFrame.from_dict(table_dict, orient='index').sort_index()
        return df

    def table_category(self, df: pd.DataFrame) -> tuple[str | None, pd.DataFrame | None]:
        if df.empty:
            return None, None

        h_idx = self.find_header_row(df, self.OWNERS_REQUIRED_COLS)
        if h_idx != -1:
            owners_df = self.set_header(df, h_idx)
            return 'owners', owners_df

        h_idx = self.find_header_row(df, self.GAP_EUL_REQUIRED_COLS)
        if h_idx != -1:
            ge_df = self.set_header(df, h_idx)
            if '등기목적' not in ge_df.columns:
                return None, None
            
            # 순위번호가 모두 비어있으면 continuation 테이블로 간주 (독립 테이블이 아님)
            if '순위번호' in ge_df.columns:
                all_ranks_empty = ge_df['순위번호'].astype(str).str.strip().eq('').all()
                if all_ranks_empty:
                    return None, None

            def label_row(purpose: str) -> str | None:
                p = self.normalize(purpose)
                if any(k in p for k in self.GAP_KEYWORDS):
                    return 'gap'
                if any(k in p for k in self.EUL_KEYWORDS):
                    return 'eul'
                return None

            cats = ge_df['등기목적'].apply(label_row)
            has_gap = any(cats == 'gap')
            has_eul = any(cats == 'eul')
            if has_gap and not has_eul:
                return 'gap_table', ge_df
            if has_eul and not has_gap:
                return 'eul_table', ge_df
            if has_gap and has_eul:
                ge_df['_cat'] = cats
                return 'gap_eul_mixed', ge_df
            return None, None

        return None, None

    def align_columns(self, df: pd.DataFrame, target_cols: list[str]) -> pd.DataFrame:
        cols_front = [c for c in target_cols]
        extra_cols = [c for c in df.columns if c not in target_cols]
        aligned = df.reindex(columns=cols_front + extra_cols)
        return aligned

    @staticmethod
    def _normalize_owner_columns(df: pd.DataFrame) -> pd.DataFrame:
        """owners 테이블 컬럼 정규화: '주 소'→'주소'"""
        if df is None or df.empty:
            return df
        out = df
        if "주 소" in out.columns and "주소" not in out.columns:
            out = out.rename(columns={"주 소": "주소"})
        return out

    def merge_split_rows(self, df: pd.DataFrame, key_col: str, concat_cols: list[str]) -> pd.DataFrame:
        """페이지 경계에서 잘린 행을 병합 (순위번호가 비어있는 행은 이전 행의 연속)"""
        if df is None or df.empty:
            return df

        for c in [key_col] + concat_cols:
            if c not in df.columns:
                df[c] = ''

        merged_rows: list[dict] = []
        last_base_key: str | None = None
        merge_count = 0

        # 식별자 컬럼은 병합 시 결합/덧붙임을 금지하고, 기존 값이 비어있을 때만 채운다 (freeze)
        id_cols_present = [c for c in ["물건번호", "지번일련번호", "지번번호"] if c in df.columns]
        safe_concat_cols = [c for c in concat_cols if c not in id_cols_present]

        # 디버그 토글 (환경변수 DBG_MERGE_IDS/DBG_MERGE_TABLE)
        try:
            import os as _os
            def _env_true(name: str) -> bool:
                v = str(_os.environ.get(name, "0")).strip().lower()
                return v in ("1", "true", "yes", "on")
            _DBG_MERGE_IDS = _env_true("DBG_MERGE_IDS")
            _DBG_MERGE_TABLE = _env_true("DBG_MERGE_TABLE")
        except Exception:
            _DBG_MERGE_IDS = False
            _DBG_MERGE_TABLE = False

        # 시작 시점 1회 요약 로그
        if _DBG_MERGE_IDS:
            try:
                _cols = list(df.columns)
                print(f"[MERGE DEBUG] start rows={len(df)} key_col='{key_col}' id_cols={id_cols_present} safe_concat={safe_concat_cols}")
                # id 컬럼이 없다면 그 사실도 알림
                if not id_cols_present:
                    print("[MERGE DEBUG] note: id columns not present in this frame (no id debug)")
            except Exception:
                pass

        # 테이블 스냅샷(마지막 호출 상태 확인용)
        if _DBG_MERGE_TABLE:
            try:
                def _selcols(frame):
                    base = [c for c in ["물건번호", key_col, "등기목적", "접수정보", "접수날짜", "주요등기사항", "대상소유자", "지번일련번호", "지번번호"] if c in frame.columns]
                    return base
                pre_cols = _selcols(df)
                if pre_cols:
                    try:
                        print("[MERGE DEBUG] table PRE (top 30):")
                        print(df[pre_cols].head(30).to_string(index=False))
                    except Exception:
                        pass
            except Exception:
                pass

        def _is_empty(v: any) -> bool:
            s = self.normalize(v)
            sl = s.lower()
            return sl == '' or sl == 'nan' or sl == 'none' or sl == '터'

        for idx, row in df.iterrows():
            key_val = self.normalize(row.get(key_col, ''))
            if key_val.lower() in ('nan', 'none'):
                key_val = ''
            # 이전 행 순위번호
            prev_key_val = self.normalize(merged_rows[-1].get(key_col, '')) if merged_rows else ''

            # owners 전용 continuation 규칙: 필수 컬럼 중 하나라도 비어있으면 이전 행에 병합
            is_owners_schema = all(c in df.columns for c in ["등기명의인", "(주민)등록번호", "최종지분", "주소", "순위번호"])
            owners_continuation = False
            if is_owners_schema:
                name_empty = _is_empty(row.get("등기명의인", ""))
                reg_empty = _is_empty(row.get("(주민)등록번호", ""))
                share_empty = _is_empty(row.get("최종지분", ""))
                address_empty = _is_empty(row.get("주소", ""))
                rank_empty = _is_empty(row.get("순위번호", ""))
                owners_continuation = name_empty or reg_empty or share_empty or address_empty or rank_empty

            # gap/eul 전용 continuation 규칙(엄격):
            # - 기본은 순위번호(key_col)가 비어있을 때만 병합
            # - 예외적으로 페이지 넘김으로 인한 본문 연속(등기목적/접수정보/대상소유자 모두 비고, 주요등기사항만 이어짐)만 병합
            is_gap_eul_schema = all(c in df.columns for c in ["순위번호", "등기목적", "접수정보", "주요등기사항", "대상소유자"])
            gap_eul_continuation = False
            if is_gap_eul_schema:
                ge_rank_empty = _is_empty(row.get("순위번호", ""))
                ge_purpose_empty = _is_empty(row.get("등기목적", ""))
                ge_receipt_empty = _is_empty(row.get("접수정보", ""))
                ge_major_empty = _is_empty(row.get("주요등기사항", ""))
                ge_owner_empty = _is_empty(row.get("대상소유자", ""))
                # 페이지 넘어감으로 판단: 목적/접수/대상은 비고, 주요등기사항만 채워져 있을 때
                content_cont = (ge_purpose_empty and ge_receipt_empty and ge_owner_empty and (not ge_major_empty))
                gap_eul_continuation = content_cont

            # 병합 규칙:
            # - 기본: 순위번호가 비어있는 연속 행만 병합
            # - owners 전용: 필수 컬럼 중 하나라도 비어 있으면 병합(주소 연속 포함)
            if is_owners_schema:
                should_merge = bool(merged_rows) and (owners_continuation or key_val == '')
            else:
                # gap/eul: 순위번호 비었거나 '본문 연속(content_cont)'인 경우만 병합
                if is_gap_eul_schema:
                    should_merge = bool(merged_rows) and ((key_val == '') or gap_eul_continuation)
                else:
                    should_merge = bool(merged_rows) and (key_val == '')
            

            purpose_val = self.normalize(row.get('등기목적', ''))
            receipt_val = self.normalize(row.get('접수정보', ''))
            owner_val = self.normalize(row.get('대상소유자', ''))

            if should_merge:
                target = merged_rows[-1]
                merge_count += 1
                
                # DEBUG: 병합 전 식별자 상태 출력
                if _DBG_MERGE_IDS and id_cols_present:
                    try:
                        prev_ids = {c: self.normalize(target.get(c, '')) for c in id_cols_present}
                        cur_ids  = {c: self.normalize(row.get(c, '')) for c in id_cols_present}
                        print(f"[MERGE DEBUG] before idx={idx} key='{key_val}' prev_ids={prev_ids} cur_ids={cur_ids}")
                    except Exception:
                        pass

                for col in safe_concat_cols:
                    prev = self.normalize(target.get(col, ''))
                    cur_raw = row.get(col, '')
                    if _is_empty(cur_raw):
                        continue
                    cur = self.normalize(cur_raw)
                    if owners_continuation and is_owners_schema:
                        # owners: 필수 값은 비어있으면 채우고, 주소는 줄바꿈 합치기
                        if col == '주소':
                            new_val = (prev + ' ' + cur).strip() if prev else cur
                            target[col] = new_val
                            continue
                        # 등기명의인/등록번호/최종지분 등은 비어있을 때만 채움
                        # (식별자 컬럼은 safe_concat_cols에서 제외되어 도달하지 않음)
                        if _is_empty(prev):
                            target[col] = cur
                        else:
                            # 중복 방지: prev에 cur이 없으면 공백으로 덧붙임
                            if cur not in prev:
                                target[col] = (prev + ' ' + cur).strip()
                        continue
                    # 일반 규칙 (gap/eul)
                    if col == '대상소유자':
                        # 대상소유자는 공백 없이 결합
                        new_val = (prev + cur) if prev else cur
                    elif col in ('접수정보', '주요등기사항'):
                        # 접수정보/주요등기사항은 공백 한 칸으로 결합
                        new_val = (prev + ' ' + cur).strip() if prev else cur
                    else:
                        # 기타 일반 텍스트도 공백 한 칸으로 결합
                        new_val = (prev + ' ' + cur).strip() if prev else cur
                    target[col] = new_val

                # 식별자 컬럼 freeze: 기존 값이 비어있을 때만 현재 행 값으로 채움, 그 외에는 변경 금지
                for idc in id_cols_present:
                    try:
                        prev_id = self.normalize(target.get(idc, ''))
                        if _is_empty(prev_id):
                            cur_id = self.normalize(row.get(idc, ''))
                            if not _is_empty(cur_id):
                                target[idc] = cur_id
                    except Exception:
                        pass

                # DEBUG: 병합 후 식별자 상태 출력
                if _DBG_MERGE_IDS and id_cols_present:
                    try:
                        new_ids = {c: self.normalize(target.get(c, '')) for c in id_cols_present}
                        print(f"[MERGE DEBUG] after  idx={idx} key='{key_val}' new_ids={new_ids}")
                    except Exception:
                        pass
                
            else:
                merged_rows.append(row.to_dict())
                last_base_key = key_val

        
        res_df = pd.DataFrame(merged_rows)
        if _DBG_MERGE_TABLE:
            try:
                def _selcols2(frame):
                    base = [c for c in ["물건번호", key_col, "등기목적", "접수정보", "접수날짜", "주요등기사항", "대상소유자", "지번일련번호", "지번번호"] if c in frame.columns]
                    return base
                post_cols = _selcols2(res_df)
                if post_cols:
                    try:
                        print("[MERGE DEBUG] table POST (top 30):")
                        print(res_df[post_cols].head(30).to_string(index=False))
                    except Exception:
                        pass
            except Exception:
                pass
        return res_df

    # ----------------------- 로컬 순위번호 재매김 -----------------------
    def renumber_gap_local(self, df: pd.DataFrame) -> pd.DataFrame:
        """단일 PDF(또는 단일 파일) 단위로 갑구 순위번호를 재매김.
        - 가압류 포함 → 'ga', 'ap'(가 아닌 '압류'), 나머지 'etc' 3그룹으로 분리 후 그룹별 재매김
        - 메인행부터 1..N 부여, 같은 기본순번의 서브행은 메인 바로 아래에 1..M 부여
        """
        if df is None or df.empty:
            return df
        out = df.copy()
        # 접수날짜 보조 생성
        if "접수날짜" not in out.columns:
            out["접수날짜"] = out.get("접수정보", "").apply(self._extract_receipt_date)
        # 순위번호 정규화
        if "순위번호" in out.columns:
            out["순위번호"] = out["순위번호"].astype(str).map(self._normalize_rank_with_prev)

        def _parse_rank_gap(rank_str: str) -> tuple[float, float]:
            try:
                s = str(rank_str).strip()
                if not s:
                    return float('inf'), float('inf')
                if '-' in s:
                    b, sub = s.split('-', 1)
                    base = pd.to_numeric(b, errors='coerce')
                    subn = pd.to_numeric(sub, errors='coerce')
                    base = float(base) if pd.notna(base) else float('inf')
                    subn = float(subn) if pd.notna(subn) else float('inf')
                    return base, subn
                base = pd.to_numeric(s, errors='coerce')
                base = float(base) if pd.notna(base) else float('inf')
                return base, float('inf')
            except Exception:
                return float('inf'), float('inf')

        def _renumber_one_group(g: pd.DataFrame) -> pd.DataFrame:
            if g is None or g.empty:
                return g
            rp = g.get("순위번호", "").apply(_parse_rank_gap)
            g["_rank_base"] = rp.apply(lambda x: x[0])
            g["_rank_sub"] = rp.apply(lambda x: x[1])
            g["_is_sub"] = (g["_rank_sub"] != float('inf')).astype(int)
            g["_orig_order"] = range(len(g))
            g["_date_ord"] = pd.to_datetime(g.get("접수날짜", ""), errors="coerce")

            # 고아 서브행 승격: 동일 base의 메인행이 존재하지 않으면 서브를 메인으로 승격
            try:
                main_bases = set(g.loc[g["_is_sub"] == 0, "_rank_base"].dropna().tolist())
                orphan_mask = (g["_is_sub"] == 1) & (~g["_rank_base"].isin(main_bases))
                if orphan_mask.any():
                    g.loc[orphan_mask, "_is_sub"] = 0
                    g.loc[orphan_mask, "_rank_sub"] = float('inf')
            except Exception:
                pass

            # 메인 먼저 정렬, 1..N 부여 (원본 순위 → 접수날짜 → 접수번호 → 원본순서)
            try:
                g["_receipt_serial"] = pd.to_numeric(
                    g.get("접수정보", "").astype(str).map(TableProcessor._extract_receipt_serial), errors="coerce"
                )
            except Exception:
                g["_receipt_serial"] = pd.NA
            
            main_rows = g[g["_is_sub"] == 0].sort_values(
                ["_rank_base", "_date_ord", "_receipt_serial", "_orig_order"],
                na_position="last", kind="mergesort"
            ).copy()
            main_rows["_new_base"] = range(1, len(main_rows) + 1)
            

            out_rows: list[dict] = []
            for _, main in main_rows.iterrows():
                base_val = main["_rank_base"]
                new_base = int(main["_new_base"])
                rec = dict(main)
                rec["_new_base"] = new_base
                rec["_sub_seq"] = None
                out_rows.append(rec)
                # 같은 base의 서브 정렬
                subs = g[(g["_is_sub"] == 1) & (g["_rank_base"] == base_val)].sort_values(["_date_ord", "_orig_order"], na_position="last")
                owner = str(main.get("대상소유자", "")).strip()
                if owner:
                    subs = subs.assign(
                        _owner_match=subs["대상소유자"].astype(str).str.strip().eq(owner).astype(int)
                    ).sort_values(["_owner_match", "_date_ord", "_orig_order"], ascending=[False, True, True])
                seq = 1
                for _, sub in subs.iterrows():
                    r = dict(sub)
                    r["_new_base"] = new_base
                    r["_sub_seq"] = seq
                    seq += 1
                    out_rows.append(r)

            g2 = pd.DataFrame(out_rows)
            def _format_row(row) -> str:
                base = int(row.get("_new_base", 0)) if pd.notna(row.get("_new_base", None)) else 0
                if pd.notna(row.get("_sub_seq", None)) and str(row.get("_sub_seq")) not in ("", "None"):
                    sub = int(row["_sub_seq"]) 
                else:
                    sub = None
                return f"{base}-{sub}" if sub is not None else str(base)
            g2["순위번호"] = g2.apply(_format_row, axis=1).astype(str)
            return g2

        def _gap_cat(purpose: str) -> str:
            s = str(purpose or "")
            s_norm = re.sub(r"\s+", "", s)
            if "가압류" in s_norm:
                return "ga"
            if re.search(r"(?<!가)압류", s_norm):
                return "ap"
            return "etc"

        out["_gap_cat"] = out.get("등기목적", "").apply(_gap_cat)
        ordered = []
        for key in ["ga", "ap", "etc"]:
            g = out[out["_gap_cat"] == key].copy()
            if g.empty:
                continue
            g = _renumber_one_group(g)
            ordered.append(g)
        ordered = [df for df in ordered if df is not None and not df.empty and df.count().sum() > 0]
        out = pd.concat(ordered, ignore_index=True) if ordered else out
        # 보조 제거
        out = out.drop(columns=[
            "_rank_base", "_rank_sub", "_is_sub", "_orig_order", "_date_ord", "_new_base", "_sub_seq", "_gap_cat"
        ], errors="ignore")
        return out

    def renumber_eul_local(self, df: pd.DataFrame) -> pd.DataFrame:
        """단일 PDF(또는 파일) 단위로 을구 순위번호를 재매김.
        - 메인 1..N → 같은 base의 서브를 메인 바로 아래 1..M 부여
        """
        if df is None or df.empty:
            return df
        out = df.copy()
        if "접수날짜" not in out.columns:
            out["접수날짜"] = out.get("접수정보", "").apply(self._extract_receipt_date)
        if "순위번호" in out.columns:
            out["순위번호"] = out["순위번호"].astype(str).map(self._normalize_rank_with_prev)

        def _parse_rank(rank_str: str) -> tuple[float, float]:
            try:
                s = str(rank_str).strip()
                if not s:
                    return float('inf'), float('inf')
                m = re.match(r"^(\d+)(?:-(\d+))?$", s)
                if not m:
                    return float('inf'), float('inf')
                base = float(m.group(1))
                sub = float(m.group(2)) if m.group(2) else float('inf')
                return base, sub
            except Exception:
                return float('inf'), float('inf')

        rp = out.get("순위번호", "").apply(_parse_rank)
        out["_rank_base"] = rp.apply(lambda x: x[0])
        out["_rank_sub"] = rp.apply(lambda x: x[1])
        out["_is_sub"] = (out["_rank_sub"] != float('inf')).astype(int)
        out["_orig_order"] = range(len(out))
        out["_date_ord"] = pd.to_datetime(out.get("접수날짜", ""), errors="coerce")
        # 고아 서브행 승격: 동일 base의 메인행이 없으면 메인으로 승격
        try:
            main_bases = set(out.loc[out["_is_sub"] == 0, "_rank_base"].dropna().tolist())
            orphan_mask = (out["_is_sub"] == 1) & (~out["_rank_base"].isin(main_bases))
            if orphan_mask.any():
                out.loc[orphan_mask, "_is_sub"] = 0
                out.loc[orphan_mask, "_rank_sub"] = float('inf')
        except Exception:
            pass
        # 접수번호(일련번호) 정렬용 보조 컬럼
        try:
            out["_receipt_serial"] = pd.to_numeric(
                out.get("접수정보", "").astype(str).map(TableProcessor._extract_receipt_serial), errors="coerce"
            )
        except Exception:
            out["_receipt_serial"] = pd.NA

        # 서브앵커 선행 생성: 같은 base에서 자신보다 앞선 메인행을 찾아
        # 서브에 메인행의 접수키/접수번호 및 대상소유자 앵커를 기록한다
        try:
            # 보조 앵커 컬럼 보장
            for c in ["메인행_접수정보", "메인행_접수번호", "메인행_접수키", "메인행_대상소유자"]:
                if c not in out.columns:
                    out[c] = ""

            sub_idx_list = out.index[out.get("_is_sub", 0) == 1].tolist()
            if sub_idx_list:
                # 메인 후보 인덱스 테이블
                mains_tbl = out[out.get("_is_sub", 0) == 0][["_rank_base", "_orig_order", "접수정보", "대상소유자"]].copy()
                for i in sub_idx_list:
                    try:
                        sub_base = out.at[i, "_rank_base"]
                        sub_order = out.at[i, "_orig_order"]
                        cands = mains_tbl[(mains_tbl["_rank_base"] == sub_base) & (mains_tbl["_orig_order"] < sub_order)]
                        if cands.empty:
                            continue
                        main_row = cands.loc[cands["_orig_order"].idxmax()]
                        main_rec = str(main_row.get("접수정보", ""))
                        try:
                            main_key = TableProcessor._build_receipt_key(main_rec)
                        except Exception:
                            main_key = main_rec
                        out.at[i, "메인행_접수정보"] = main_rec
                        out.at[i, "메인행_접수번호"] = main_key
                        out.at[i, "메인행_접수키"] = main_key
                        try:
                            out.at[i, "메인행_대상소유자"] = str(main_row.get("대상소유자", "")).replace("\u200B", "").strip()
                        except Exception:
                            pass
                    except Exception:
                        pass
        except Exception:
            pass

        # 디버그 플래그(환경변수): 메인/서브 매칭 키 출력 제어
        _DBG_MATCH_KEYS = False
        try:
            import os as _os
            def _env_true(name: str) -> bool:
                v = str(_os.environ.get(name, "0")).strip().lower()
                return v in ("1", "true", "yes", "on")
            _DBG_MATCH_KEYS = _env_true("DBG_MATCH_KEYS")
        except Exception:
            _DBG_MATCH_KEYS = False

        # 메인행 정렬: 접수날짜 → 접수번호 → 원본순서 (순위 기반 제거)
        main_rows = out[out["_is_sub"] == 0].sort_values(
            ["_date_ord", "_receipt_serial", "_orig_order"],
            na_position="last", kind="mergesort"
        ).copy()
        main_rows["_new_base"] = range(1, len(main_rows) + 1)
        

        rows: list[dict] = []
        # 서브 풀과 사용된 인덱스 집합을 유지하여 중복 부착 방지
        subs_pool = out[out["_is_sub"] == 1].copy()
        used_sub_idx: set[int] = set()
        for _, main in main_rows.iterrows():
            base_val = main["_rank_base"]
            new_base = int(main["_new_base"])
            rec = dict(main)
            rec["_new_base"] = new_base
            rec["_sub_seq"] = None
            rows.append(rec)
            # 서브 후보: 우선 메인행 앵커(메인행_접수번호/메인행_접수키)로 정확 매칭
            try:
                own_key = TableProcessor._build_receipt_key(main.get("접수정보", ""))
            except Exception:
                own_key = str(main.get("접수정보", ""))
            subs_all = subs_pool.loc[~subs_pool.index.isin(used_sub_idx)].copy()
            subs = pd.DataFrame(columns=out.columns)
            if _DBG_MATCH_KEYS:
                try:
                    _owner_dbg = str(main.get("대상소유자", "")).replace("\u200B", "").strip()
                    print(f"[MATCH DEBUG][EUL] main base={base_val} own_key={own_key} owner={_owner_dbg}")
                    _cols = [c for c in ["메인행_접수번호", "메인행_접수키", "메인행_대상소유자", "대상소유자", "접수정보"] if c in subs_all.columns]
                    if _cols and not subs_all.empty:
                        print("[MATCH DEBUG][EUL] sub anchors preview:")
                        print(subs_all[_cols].head(10).to_string(index=False))
                except Exception:
                    pass
            # 키 매칭: 메인행 own_key와 서브의 메인행_접수번호/메인행_접수키 중 하나 일치
            key_mask = None
            if "메인행_접수번호" in subs_all.columns and own_key:
                key_mask = subs_all["메인행_접수번호"].astype(str) == str(own_key)
            if "메인행_접수키" in subs_all.columns and own_key:
                mask2 = subs_all["메인행_접수키"].astype(str) == str(own_key)
                key_mask = mask2 if key_mask is None else (key_mask | mask2)

            # 소유자 매칭: 우선 메인행_대상소유자, 없으면 대상소유자 비교 (공백/ZWSP 제거)
            def _norm(s: pd.Series) -> pd.Series:
                return s.astype(str).str.replace("\u200B", "").str.replace(r"\s+", "", regex=True)
            owner_norm = _norm(pd.Series([owner])).iloc[0]
            owner_mask = None
            if "메인행_대상소유자" in subs_all.columns:
                owner_mask = _norm(subs_all["메인행_대상소유자"]) == owner_norm
            if owner_mask is None and "대상소유자" in subs_all.columns:
                owner_mask = _norm(subs_all["대상소유자"]) == owner_norm

            # 최종: 키 AND 소유자 모두 일치하는 서브만 부착 (폴백 금지)
            if key_mask is not None and owner_mask is not None:
                subs = subs_all[key_mask & owner_mask].copy()
            else:
                subs = pd.DataFrame(columns=subs_all.columns)

            subs = subs.sort_values(["_date_ord", "_orig_order"], na_position="last")
            owner = str(main.get("대상소유자", "")).strip()
            if _DBG_MATCH_KEYS:
                try:
                    print(f"[MATCH DEBUG][EUL] matched_by_key_n={len(subs)} pool_n={len(subs_all)} owner_filter={(owner != '')}")
                except Exception:
                    pass
            if owner:
                subs = subs.assign(
                    _owner_match=subs["대상소유자"].astype(str).str.strip().eq(owner).astype(int)
                ).sort_values(["_owner_match", "_date_ord", "_orig_order"], ascending=[False, True, True])
            seq = 1
            for _, sub in subs.iterrows():
                # 이미 다른 메인에 부착된 서브는 건너뜀
                if sub.name in used_sub_idx:
                    continue
                if _DBG_MATCH_KEYS:
                    try:
                        _sak = str(sub.get("메인행_접수번호", "")) or str(sub.get("메인행_접수키", ""))
                        _sao = str(sub.get("메인행_대상소유자", "")) or str(sub.get("대상소유자", ""))
                        print(f"[MATCH DEBUG][EUL] attach sub_idx={getattr(sub, 'name', '-')} key={_sak} owner={_sao}")
                    except Exception:
                        pass
                r = dict(sub)
                r["_new_base"] = new_base
                r["_sub_seq"] = seq
                # 서브행에 로컬 기준 부모 접수정보/번호를 함께 기록해 전역 병합 이후에도 보존
                try:
                    main_rec_info = str(main.get("접수정보", ""))
                    r["메인행_접수정보"] = main_rec_info
                except Exception:
                    r["메인행_접수정보"] = str(main.get("접수정보", ""))
                try:
                    main_rec_key = TableProcessor._build_receipt_key(main.get("접수정보", ""))
                except Exception:
                    main_rec_key = str(main.get("접수정보", ""))
                r["메인행_접수번호"] = main_rec_key
                r["메인행_접수키"] = main_rec_key
                seq += 1
                rows.append(r)
                used_sub_idx.add(sub.name)

        out2 = pd.DataFrame(rows)
        def _fmt(row) -> str:
            base = int(row.get("_new_base", 0)) if pd.notna(row.get("_new_base", None)) else 0
            if pd.notna(row.get("_sub_seq", None)) and str(row.get("_sub_seq")) not in ("", "None"):
                sub = int(row["_sub_seq"]) 
            else:
                sub = None
            return f"{base}-{sub}" if sub is not None else str(base)
        out2["순위번호"] = out2.apply(_fmt, axis=1).astype(str)
        
        return out2.drop(columns=["_rank_base", "_rank_sub", "_is_sub", "_orig_order", "_date_ord", "_new_base", "_sub_seq"], errors="ignore")

    def remove_spaces_in_columns(self, df: pd.DataFrame, col_names: list[str]) -> pd.DataFrame:
        if df is None or df.empty:
            return df
        for col in col_names:
            if col in df.columns:
                df[col] = df[col].astype(str).str.replace(r"\s+", "", regex=True)
        return df

    def run(self, input_json_path: str):
        with open(input_json_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        owners_list: list[pd.DataFrame] = []
        gap_list: list[pd.DataFrame] = []
        eul_list: list[pd.DataFrame] = []

        for page in data:
            for image in page.get('images', []):
                tables_data = image.get('tables', [])
                for table in tables_data:
                    cells = table.get('cells', [])
                    raw_df = self.build_df_from_cells(cells)
                    cat, df_cat = self.table_category(raw_df)
                    
                    # 테이블 분류 실패 시, continuation 테이블인지 확인
                    if df_cat is None or df_cat.empty:
                        # GAP_EUL_REQUIRED_COLS 헤더를 가진 테이블인지 확인
                        h_idx = self.find_header_row(raw_df, self.GAP_EUL_REQUIRED_COLS)
                        if h_idx != -1:
                            df_continuation = self.set_header(raw_df, h_idx)
                            # 순위번호가 모두 비어있고, 다른 컬럼에 데이터가 있으면 연속 행으로 간주
                            if '순위번호' in df_continuation.columns:
                                all_ranks_empty = df_continuation['순위번호'].astype(str).str.strip().eq('').all()
                                has_other_data = False
                                for col in ['접수정보', '주요등기사항', '대상소유자']:
                                    if col in df_continuation.columns:
                                        if df_continuation[col].astype(str).str.strip().ne('').any():
                                            has_other_data = True
                                            break
                                
                                if all_ranks_empty and has_other_data:
                                    
                                    # 이전 을구 또는 갑구 파트에 병합
                                    if eul_list:
                                        
                                        # 빈/전부-NA 테이블 제외 후 concat
                                        cand = [eul_list[-1], df_continuation]
                                        cand = [d for d in cand if d is not None and not d.empty and d.count().sum() > 0]
                                        eul_list[-1] = pd.concat(cand, ignore_index=True) if cand else eul_list[-1]
                                        # concat 후 다시 merge_split_rows 실행하여 행 병합
                                        eul_list[-1] = self.merge_split_rows(
                                            eul_list[-1],
                                            key_col='순위번호',
                                            concat_cols=[c for c in self.GAP_EUL_TARGET_COLS if c != '순위번호']
                                        )
                                        continue
                                    elif gap_list:
                                        
                                        cand = [gap_list[-1], df_continuation]
                                        cand = [d for d in cand if d is not None and not d.empty and d.count().sum() > 0]
                                        gap_list[-1] = pd.concat(cand, ignore_index=True) if cand else gap_list[-1]
                                        # concat 후 다시 merge_split_rows 실행하여 행 병합
                                        gap_list[-1] = self.merge_split_rows(
                                            gap_list[-1],
                                            key_col='순위번호',
                                            concat_cols=[c for c in self.GAP_EUL_TARGET_COLS if c != '순위번호']
                                        )
                                        continue
                        # continuation도 아니면 스킵
                        continue
                    
                    if cat == 'owners' and df_cat is not None and not df_cat.empty:
                        df_cat = self._normalize_owner_columns(df_cat)
                        df_cat = df_cat.reset_index(drop=True)
                        # owners continuation: 첫 행의 순위번호가 비어있고, 다른 핵심 컬럼은 있는 경우 → 직전 owners에 병합
                        try:
                            all_ranks_empty = '순위번호' in df_cat.columns and df_cat['순위번호'].astype(str).str.strip().eq('').all()
                            has_other_data = any(
                                df_cat.get(col, '').astype(str).str.strip().ne('').any()
                                for col in ["등기명의인", "(주민)등록번호", "최종지분", "주소"] if col in df_cat.columns
                            )
                        except Exception:
                            all_ranks_empty = False
                            has_other_data = False

                        if all_ranks_empty and has_other_data and owners_list:
                            cand = [owners_list[-1], df_cat]
                            cand = [d for d in cand if d is not None and not d.empty and d.count().sum() > 0]
                            owners_list[-1] = pd.concat(cand, ignore_index=True) if cand else owners_list[-1]
                            owners_list[-1] = self.merge_split_rows(
                                owners_list[-1],
                                key_col='순위번호',
                                concat_cols=[c for c in self.OWNERS_TARGET_COLS if c != '순위번호']
                            )
                        else:
                            owners_list.append(df_cat)
                    elif cat == 'gap_table' and df_cat is not None and not df_cat.empty:
                        df_cat = df_cat.reset_index(drop=True)
                        # 테이블 첫 행이 순위번호 비어있으면 이전 gap 파트의 마지막 행에 병합
                        try:
                            if '순위번호' in df_cat.columns and str(df_cat.iloc[0]['순위번호']).strip() == '' and gap_list:
                                prev_df = gap_list[-1]
                                if prev_df is not None and not prev_df.empty:
                                    # 병합 대상 컬럼 규칙: 대상소유자는 공백 없이, 나머지는 공백 추가
                                    concat_cols = [c for c in self.GAP_EUL_TARGET_COLS if c != '순위번호' and c in df_cat.columns]
                                    for col in concat_cols:
                                        prev_val = str(prev_df.iloc[-1].get(col, '')).strip()
                                        cur_val = str(df_cat.iloc[0].get(col, '')).strip()
                                        if not cur_val:
                                            continue
                                        if col == '대상소유자':
                                            new_val = (prev_val + cur_val) if prev_val else cur_val
                                        else:
                                            new_val = (prev_val + ' ' + cur_val).strip() if prev_val else cur_val
                                        prev_df.iat[len(prev_df.index)-1, prev_df.columns.get_loc(col)] = new_val
                                    # 병합된 첫 행 제거
                                    df_cat = df_cat.iloc[1:].reset_index(drop=True)
                        except Exception:
                            pass
                        if df_cat is not None and not df_cat.empty:
                            gap_list.append(df_cat)
                    elif cat == 'eul_table' and df_cat is not None and not df_cat.empty:
                        df_cat = df_cat.reset_index(drop=True)
                        # 테이블 첫 행이 순위번호 비어있으면 이전 eul 파트의 마지막 행에 병합
                        try:
                            if '순위번호' in df_cat.columns and str(df_cat.iloc[0]['순위번호']).strip() == '' and eul_list:
                                prev_df = eul_list[-1]
                                if prev_df is not None and not prev_df.empty:
                                    concat_cols = [c for c in self.GAP_EUL_TARGET_COLS if c != '순위번호' and c in df_cat.columns]
                                    for col in concat_cols:
                                        prev_val = str(prev_df.iloc[-1].get(col, '')).strip()
                                        cur_val = str(df_cat.iloc[0].get(col, '')).strip()
                                        if not cur_val:
                                            continue
                                        if col == '대상소유자':
                                            new_val = (prev_val + cur_val) if prev_val else cur_val
                                        else:
                                            new_val = (prev_val + ' ' + cur_val).strip() if prev_val else cur_val
                                        prev_df.iat[len(prev_df.index)-1, prev_df.columns.get_loc(col)] = new_val
                                    df_cat = df_cat.iloc[1:].reset_index(drop=True)
                        except Exception:
                            pass
                        if df_cat is not None and not df_cat.empty:
                            eul_list.append(df_cat)
                    elif cat == 'gap_eul_mixed' and df_cat is not None and not df_cat.empty:
                        if '_cat' in df_cat.columns:
                            if any(df_cat['_cat'] == 'gap'):
                                gap_part = df_cat[df_cat['_cat'] == 'gap'].drop(columns=['_cat'])
                                gap_list.append(gap_part.reset_index(drop=True))
                            if any(df_cat['_cat'] == 'eul'):
                                eul_part = df_cat[df_cat['_cat'] == 'eul'].drop(columns=['_cat'])
                                eul_list.append(eul_part.reset_index(drop=True))

        if owners_list:
            cand = [d for d in owners_list if d is not None and not d.empty and d.count().sum() > 0]
            owners_merged = pd.concat(cand, ignore_index=True).fillna('') if cand else pd.DataFrame()
            owners_merged = self.align_columns(owners_merged, self.OWNERS_TARGET_COLS)
            # 페이지 경계에서 잘린 행 병합 보정
            owners_merged = self.merge_split_rows(
                owners_merged,
                key_col='순위번호',
                concat_cols=[c for c in self.OWNERS_TARGET_COLS if c != '순위번호']
            )
            owners_merged = self.remove_spaces_in_columns(owners_merged, ["등기명의인"])  # 요청 반영
            owners_merged.to_csv(self.OWNERS_CSV, encoding="utf-8-sig", index=False, header=True)

        if gap_list:
            cand = [d for d in gap_list if d is not None and not d.empty and d.count().sum() > 0]
            gap_merged = pd.concat(cand, ignore_index=True).fillna('') if cand else pd.DataFrame()
            gap_merged = self.align_columns(gap_merged, self.GAP_EUL_TARGET_COLS)
            gap_merged = self.remove_spaces_in_columns(gap_merged, ["대상소유자"])  # 요청 반영
            # 병합 시 식별자 컬럼은 결합 대상에서 제외(물건번호/지번일련번호/지번번호)
            _gap_concat_cols = [
                c for c in self.GAP_EUL_TARGET_COLS
                if c not in ('순위번호', '물건번호', '지번일련번호', '지번번호')
            ]
            gap_merged = self.merge_split_rows(
                gap_merged,
                key_col='순위번호',
                concat_cols=_gap_concat_cols
            )
            # 갑구 파싱 함수 적용
            # 접수정보 원문 대신 정규화된 '_접수키'를 dedup 기준에 포함하여 동일 접수 병합을 보장
            try:
                gap_merged["_접수키"] = gap_merged.get("접수정보", "").astype(str).map(self._build_receipt_key)
            except Exception:
                gap_merged["_접수키"] = ""
            # 기존 기준에서 '순위번호', '접수정보'는 제외하고 '_접수키'를 추가
            _content_cols = [c for c in self.GAP_EUL_TARGET_COLS if c not in ('순위번호', '접수정보')]
            if "_접수키" not in _content_cols:
                _content_cols.append("_접수키")
            gap_dedup = self._dedup_with_remark(gap_merged, _content_cols)
            gap_parsed = self.parse_gap(gap_dedup)
            # [DEBUG] 최종 저장 직전, (물건번호, _접수키) 중복 여부 점검
            try:
                import os as _os
                _dbg_item = _os.environ.get("DBG_GAP_ITEM", "R-019_01")
                if {"물건번호", "접수정보"}.issubset(gap_parsed.columns):
                    _tmp = gap_parsed.copy()
                    _tmp["_접수키"] = _tmp["접수정보"].astype(str).map(self._build_receipt_key)
                    _dups = (
                        _tmp.groupby(["물건번호", "_접수키"], dropna=False)
                            .size()
                            .reset_index(name="cnt")
                    )
                    _dups = _dups[_dups["cnt"] > 1].sort_values(["물건번호", "_접수키"], ascending=[True, True])
                    pass
                    # 특정 item 필터 요약
                    _dups_item = _dups[_dups["물건번호"].astype(str) == str(_dbg_item)]
                    pass
            except Exception:
                pass
            gap_parsed.to_csv(self.GAPGU_CSV, encoding="utf-8-sig", index=False, header=True)

        if eul_list:
            cand = [d for d in eul_list if d is not None and not d.empty and d.count().sum() > 0]
            eul_merged = pd.concat(cand, ignore_index=True).fillna('') if cand else pd.DataFrame()
            eul_merged = self.align_columns(eul_merged, self.GAP_EUL_TARGET_COLS)
            eul_merged = self.remove_spaces_in_columns(eul_merged, ["대상소유자"])  # 요청 반영
            _eul_concat_cols = [
                c for c in self.GAP_EUL_TARGET_COLS
                if c not in ('순위번호', '물건번호', '지번일련번호', '지번번호')
            ]
            eul_merged = self.merge_split_rows(
                eul_merged,
                key_col='순위번호',
                concat_cols=_eul_concat_cols
            )
            # 을구 파싱 함수 적용
            eul_dedup = self._dedup_with_remark(eul_merged, [c for c in self.GAP_EUL_TARGET_COLS if c != '순위번호'])
            eul_parsed = self.parse_eul(eul_dedup)
            eul_parsed.to_csv(self.EULGU_CSV, encoding="utf-8-sig", index=False, header=True)

    # ----------------------- 물건번호 기준 집계/병합 -----------------------
    @staticmethod
    def _parse_ids_from_filename(path: str) -> tuple[str, str | None]:
        """파일명에서 물건번호(R-###_##)와 지번일련번호(R-###_##_##) 추출"""
        stem = os.path.splitext(os.path.basename(path))[0]
        first_tok = stem.split()[0] if stem else ''
        m = re.match(r"^((?:R|S)-\d{3})[_-]([0-9]{1,2})(?:[_-]([0-9]{1,2}))?$", first_tok)
        if m:
            base_id = f"{m.group(1)}_{int(m.group(2)):02d}"
            full_id = f"{base_id}_{int(m.group(3)):02d}" if m.group(3) else None
            return base_id, full_id
        ft = first_tok.replace('-', '_')
        m2 = re.match(r"^((?:R|S)-\d{3})_([0-9]{1,2})(?:_([0-9]{1,2}))?$", ft)
        if m2:
            base_id = f"{m2.group(1)}_{int(m2.group(2)):02d}"
            full_id = f"{base_id}_{int(m2.group(3)):02d}" if m2.group(3) else None
            return base_id, full_id
        return first_tok.strip(), None

    @staticmethod
    def _address_from_filename(path: str) -> str:
        """파일명에서 주소 유사 문자열을 추출: 첫 토큰(ID)과 다음 대괄호 토큰을 제외한 나머지"""
        try:
            stem = os.path.splitext(os.path.basename(path))[0]
            tokens = stem.split()
            if not tokens:
                return ""
            if len(tokens) >= 3 and tokens[1].startswith("[") and tokens[1].endswith("]"):
                addr = " ".join(tokens[2:])
            else:
                addr = " ".join(tokens[1:])
            return addr.strip()
        except Exception:
            return ""

    @staticmethod
    def _ensure_columns(df: pd.DataFrame, cols: list[str]) -> pd.DataFrame:
        out = df.copy()
        for c in cols:
            if c not in out.columns:
                out[c] = ''
        return out

    def _dedup_with_remark(self, df: pd.DataFrame, content_cols: list[str], id_col: str = "지번일련번호") -> pd.DataFrame:
        """
        같은 물건번호 내 content_cols가 동일한 행은 중복 제거.
        - 동일 내용이 여러 지번에서 나오면 1행만 유지하고 '비고'는 공란
        - 단 하나의 지번에서만 나오면 '비고'에 해당 지번일련번호 기입
        """
        if df is None or df.empty:
            return df
        cols_needed = ["물건번호", id_col] + content_cols
        df2 = self._ensure_columns(df, cols_needed)
        rows: list[dict] = []
        group_key = ["물건번호"] + content_cols
        for _, g in df2.groupby(group_key, dropna=False):
            ids = [str(v).strip() for v in g[id_col].astype(str).tolist() if str(v).strip()]
            uniq_ids = sorted(set(ids))
            # 지번번호들: 지번일련번호의 마지막 세그먼트 모음 (정렬, 중복 제거)
            def _suffix(v: str) -> str:
                parts = re.split(r"[_-]", v)
                return parts[-1] if parts else ""
            suffixes = [s for s in sorted(set(_suffix(v) for v in uniq_ids)) if s]

            row = g.iloc[0].copy()
            row["비고"] = (uniq_ids[0] if len(uniq_ids) == 1 else "")
            row["지번번호들"] = ", ".join(suffixes)
            rows.append(row.to_dict())
        return pd.DataFrame(rows)

    def aggregate_from_json_paths(self, json_paths: list[str]) -> tuple[pd.DataFrame | None, pd.DataFrame | None, pd.DataFrame | None]:
        """
        여러 JSON 파일을 받아 owners/gapgu/eul을 지번일련번호 기준으로 집계.
        - 각 표에 '물건번호', '지번일련번호' 컬럼을 부여
        - 내용 중복 제거 및 '비고' 컬럼에 단건 지번 표기
        - 결과는 각 테이블별 DataFrame 반환(없으면 None)
        """
        owners_parts: list[pd.DataFrame] = []
        gap_parts: list[pd.DataFrame] = []
        eul_parts: list[pd.DataFrame] = []

        # 지번일련번호 자동 배정 카운터 (전 카테고리 공통)
        auto_jibeon_counter: dict[str, int] = {}
        # 주소 기반 지번일련번호 카운터: (물건번호, norm(address)) → next suffix
        addr_counters: dict[tuple[str, str], int] = {}
        # 주소 기반 사용된 접미 저장: (물건번호, norm(address)) → {used suffix ints}
        addr_used_suffixes: dict[tuple[str, str], set[int]] = {}

        for path in json_paths:
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    raw = json.load(f)
                # 우선순위 1) JSON 내부 itemId 사용, 2) 파일명에서 추정
                item_id_from_json = None
                address_from_json = None
                jibeon_from_json = None
                if isinstance(raw, dict):
                    item_id_from_json = str(raw.get('itemId') or '').strip() or None
                    address_from_json = str(raw.get('address') or '').strip() or None
                    jibeon_from_json = str(raw.get('jibeonId') or '').strip() or None
                # 호환: 객체형 { pages: [...] } 또는 리스트형 [ ... ] 모두 지원
                if isinstance(raw, dict) and 'pages' in raw:
                    data = raw.get('pages') or []
                else:
                    data = raw if isinstance(raw, list) else []
            except Exception:
                continue
            # 파일명 의존 제거: JSON 값만 사용
            item_id = item_id_from_json
            jibeon_id = jibeon_from_json
            # address는 OCR JSON의 address만 사용

            # 파일 단위 지번일련번호 확정: JSON만 신뢰. 제공 없으면 공란 유지
            iid = item_id or ''
            resolved_jibeon_for_file = jibeon_id or ''

            # 파일 단위 버퍼: 본 파일에서 생성된 테이블만 임시로 담아 최종 병합 후 전역에 추가
            file_gap_tables: list[pd.DataFrame] = []
            file_eul_tables: list[pd.DataFrame] = []
            file_owners_tables: list[pd.DataFrame] = []

            # 전역 파트 리스트에서 본 파일의 시작 위치 저장
            gap_start_idx = len(gap_parts)
            eul_start_idx = len(eul_parts)

            # 헤더가 있지만 내용이 순위번호 없는 1~2행인 테이블을 이전 테이블에 병합하기 위한 버퍼
            last_gap_part = None
            last_eul_part = None
            
            for page_idx, page in enumerate(data):
                for image in page.get('images', []):
                    for table_idx, table in enumerate(image.get('tables', [])):
                        raw_df = self.build_df_from_cells(table.get('cells', []))
                        
                        
                        cat, df_cat = self.table_category(raw_df)
                        
                        # 테이블 분류 실패 시, 헤더가 있고 순위번호가 비어있는 연속 행인지 확인
                        if df_cat is None or df_cat.empty:
                            # GAP_EUL_REQUIRED_COLS 헤더를 가진 테이블인지 확인
                            h_idx = self.find_header_row(raw_df, self.GAP_EUL_REQUIRED_COLS)
                            if h_idx != -1:
                                df_continuation = self.set_header(raw_df, h_idx)
                                # 순위번호가 모두 비어있고, 다른 컬럼에 데이터가 있으면 연속 행으로 간주
                                if '순위번호' in df_continuation.columns:
                                    all_ranks_empty = df_continuation['순위번호'].astype(str).str.strip().eq('').all()
                                    has_other_data = False
                                    for col in ['접수정보', '주요등기사항', '대상소유자']:
                                        if col in df_continuation.columns:
                                            if df_continuation[col].astype(str).str.strip().ne('').any():
                                                has_other_data = True
                                                break
                                    
                                    
                                    if all_ranks_empty and has_other_data:
                                        # 파일 단위 버퍼의 마지막 표에 병합
                                        if 'file_eul_tables' in locals() and file_eul_tables:
                                            cand = [file_eul_tables[-1], df_continuation]
                                            cand = [d for d in cand if d is not None and not d.empty and d.count().sum() > 0]
                                            file_eul_tables[-1] = pd.concat(cand, ignore_index=True) if cand else file_eul_tables[-1]
                                            file_eul_tables[-1] = self.merge_split_rows(
                                                file_eul_tables[-1],
                                                key_col='순위번호',
                                                concat_cols=[c for c in file_eul_tables[-1].columns if c != '순위번호']
                                            )
                                            last_eul_part = file_eul_tables[-1]
                                            continue
                                        if 'file_gap_tables' in locals() and file_gap_tables:
                                            cand = [file_gap_tables[-1], df_continuation]
                                            cand = [d for d in cand if d is not None and not d.empty and d.count().sum() > 0]
                                            file_gap_tables[-1] = pd.concat(cand, ignore_index=True) if cand else file_gap_tables[-1]
                                            file_gap_tables[-1] = self.merge_split_rows(
                                                file_gap_tables[-1],
                                                key_col='순위번호',
                                                concat_cols=[c for c in self.GAP_EUL_TARGET_COLS if c != '순위번호']
                                            )
                                            last_gap_part = file_gap_tables[-1]
                                            continue
                        
                        if df_cat is None or df_cat.empty:
                            continue
                        df_cat = df_cat.reset_index(drop=True).copy()
                        df_cat["물건번호"] = iid
                        # 파일 단위로 확정된 지번일련번호를 해당 파일 내 모든 테이블/행에 동일 적용
                        df_cat["지번일련번호"] = resolved_jibeon_for_file
                        # 등본 주소 보조 키 보관
                        if address_from_json:
                            df_cat["OCR 주소"] = address_from_json

                        if cat == 'owners':
                            df_cat = self._normalize_owner_columns(df_cat)
                            df_cat = self.align_columns(df_cat, self.OWNERS_TARGET_COLS)
                            # 페이지 경계에서 잘린 행 병합 (표 내부)
                            df_cat = self.merge_split_rows(
                                df_cat,
                                key_col='순위번호',
                                concat_cols=[c for c in self.OWNERS_TARGET_COLS if c != '순위번호']
                            )
                            # 파일 단위 owners 버퍼에 누적 (표-사이 continuation 처리를 위해)
                            if file_owners_tables:
                                # 직전 표와 이어붙인 뒤 즉시 병합 실행
                                cand = [file_owners_tables[-1], df_cat]
                                cand = [d for d in cand if d is not None and not d.empty and d.count().sum() > 0]
                                file_owners_tables[-1] = pd.concat(cand, ignore_index=True) if cand else file_owners_tables[-1]
                                file_owners_tables[-1] = self.merge_split_rows(
                                    file_owners_tables[-1],
                                    key_col='순위번호',
                                    concat_cols=[c for c in self.OWNERS_TARGET_COLS if c != '순위번호']
                                )
                            else:
                                file_owners_tables.append(df_cat)
                        elif cat == 'gap_table':
                            df_cat = self.align_columns(df_cat, self.GAP_EUL_TARGET_COLS)
                            # 페이지 경계에서 잘린 행 병합
                            _cc_gap = [c for c in df_cat.columns if c not in ('순위번호','물건번호','지번일련번호','지번번호')]
                            df_cat = self.merge_split_rows(
                                df_cat,
                                key_col='순위번호',
                                concat_cols=_cc_gap
                            )
                            # 표-사이 경계 continuation 추가 처리 (gap)
                            try:
                                if not df_cat.empty and file_gap_tables:
                                    def _is_cont_row(row: pd.Series) -> bool:
                                        def _empty(x):
                                            s = str(x or '').strip().lower()
                                            return s == '' or s == 'nan' or s == 'none'
                                        return (
                                            _empty(row.get('순위번호', '')) or
                                            _empty(row.get('등기목적', '')) or
                                            _empty(row.get('접수정보', '')) or
                                            _empty(row.get('주요등기사항', '')) or
                                            _empty(row.get('대상소유자', ''))
                                        )
                                    if _is_cont_row(df_cat.iloc[0]):
                                        cand = [file_gap_tables[-1], df_cat]
                                        cand = [d for d in cand if d is not None and not d.empty and d.count().sum() > 0]
                                        merged = pd.concat(cand, ignore_index=True) if cand else file_gap_tables[-1]
                                        _cc_merged = [c for c in merged.columns if c not in ('순위번호','물건번호','지번일련번호','지번번호')]
                                        merged = self.merge_split_rows(
                                            merged,
                                            key_col='순위번호',
                                            concat_cols=_cc_merged
                                        )
                                        file_gap_tables[-1] = merged
                                        # 이미 병합했으므로 다음 테이블로 진행
                                        
                                        continue
                            except Exception:
                                pass
                            # 주의: gap은 표-사이 continuation을 모두 처리한 뒤(파일 단위 통합본에서) 한 번만 재매김
                            file_gap_tables.append(df_cat)
                        elif cat == 'eul_table':
                            df_cat = self.align_columns(df_cat, self.GAP_EUL_TARGET_COLS)
                            # 페이지 경계에서 잘린 행 병합
                            df_cat = self.merge_split_rows(
                                df_cat,
                                key_col='순위번호',
                                concat_cols=[c for c in df_cat.columns if c != '순위번호']
                            )
                            # 표-사이 경계에서 넘어온 continuation인지 추가 확인
                            try:
                                if not df_cat.empty and file_eul_tables:
                                    def _is_cont_row(row: pd.Series) -> bool:
                                        def _empty(x):
                                            s = str(x or '').strip().lower()
                                            return s == '' or s == 'nan' or s == 'none'
                                        return (
                                            _empty(row.get('순위번호', '')) or
                                            _empty(row.get('등기목적', '')) or
                                            _empty(row.get('접수정보', '')) or
                                            _empty(row.get('주요등기사항', '')) or
                                            _empty(row.get('대상소유자', ''))
                                        )
                                    if _is_cont_row(df_cat.iloc[0]):
                                        
                                        cand = [file_eul_tables[-1], df_cat]
                                        cand = [d for d in cand if d is not None and not d.empty and d.count().sum() > 0]
                                        merged = pd.concat(cand, ignore_index=True) if cand else file_eul_tables[-1]
                                        merged = self.merge_split_rows(
                                            merged,
                                            key_col='순위번호',
                                            concat_cols=[c for c in merged.columns if c != '순위번호']
                                        )
                                        file_eul_tables[-1] = merged
                                        # 다음 테이블로 이동 (이미 병합됨)
                                        
                                        continue
                            except Exception:
                                pass
                            # 주의: 을구도 표-사이 continuation/행병합을 모두 처리한 뒤(파일 단위 통합본에서) 한 번만 재매김
                            file_eul_tables.append(df_cat)
                        elif cat == 'gap_eul_mixed':
                            if '_cat' in df_cat.columns:
                                if any(df_cat['_cat'] == 'gap'):
                                    gap_part = df_cat[df_cat['_cat'] == 'gap'].drop(columns=['_cat'])
                                    gap_part = self.align_columns(gap_part, self.GAP_EUL_TARGET_COLS)
                                    gap_part = self.merge_split_rows(
                                        gap_part,
                                        key_col='순위번호',
                                        concat_cols=[c for c in self.GAP_EUL_TARGET_COLS if c != '순위번호']
                                    )
                                    file_gap_tables.append(gap_part)
                                if any(df_cat['_cat'] == 'eul'):
                                    eul_part = df_cat[df_cat['_cat'] == 'eul'].drop(columns=['_cat'])
                                    eul_part = self.align_columns(eul_part, self.GAP_EUL_TARGET_COLS)
                                    eul_part = self.merge_split_rows(
                                        eul_part,
                                        key_col='순위번호',
                                        concat_cols=[c for c in self.GAP_EUL_TARGET_COLS if c != '순위번호']
                                    )
                                    file_eul_tables.append(eul_part)

            # 파일 단위 최종 테이블 확정: tmp_gap/tmp_eul
            try:
                if file_gap_tables:
                    cand = [d for d in file_gap_tables if d is not None and not d.empty and d.count().sum() > 0]
                    tmp_gap = pd.concat(cand, ignore_index=True) if cand else pd.DataFrame()
                    tmp_gap = self.align_columns(tmp_gap, self.GAP_EUL_TARGET_COLS)
                    # gap/eul: 표 단위에서 이미 페이지 연속 병합을 수행 → 여기서는 추가 병합을 하지 않음
                    # 파일 단위 통합본에 대해 한 번 더 로컬 재매김(표 사이 순서 보정)
                    try:
                        tmp_gap = self.renumber_gap_local(tmp_gap)
                    except Exception:
                        pass
                    # 최종 대상소유자 공백 제거
                    try:
                        if '대상소유자' in tmp_gap.columns:
                            tmp_gap['대상소유자'] = tmp_gap['대상소유자'].astype(str).str.replace(r"\s+", "", regex=True)
                    except Exception:
                        pass
                    gap_parts.append(tmp_gap)
                if file_eul_tables:
                    cand = [d for d in file_eul_tables if d is not None and not d.empty and d.count().sum() > 0]
                    tmp_eul = pd.concat(cand, ignore_index=True) if cand else pd.DataFrame()
                    tmp_eul = self.align_columns(tmp_eul, self.GAP_EUL_TARGET_COLS)
                    # gap/eul: 표 단위에서 이미 페이지 연속 병합을 수행 → 여기서는 추가 병합을 하지 않음
                    try:
                        tmp_eul = self.renumber_eul_local(tmp_eul)
                    except Exception:
                        pass
                    # 최종 대상소유자 공백 제거
                    try:
                        if '대상소유자' in tmp_eul.columns:
                            tmp_eul['대상소유자'] = tmp_eul['대상소유자'].astype(str).str.replace(r"\s+", "", regex=True)
                    except Exception:
                        pass
                    eul_parts.append(tmp_eul)
                if file_owners_tables:
                    cand = [d for d in file_owners_tables if d is not None and not d.empty and d.count().sum() > 0]
                    tmp_owners = pd.concat(cand, ignore_index=True) if cand else pd.DataFrame()
                    tmp_owners = self.align_columns(tmp_owners, self.OWNERS_TARGET_COLS)
                    tmp_owners = self.merge_split_rows(
                        tmp_owners,
                        key_col='순위번호',
                        concat_cols=[c for c in self.OWNERS_TARGET_COLS if c != '순위번호']
                    )
                    owners_parts.append(tmp_owners)
            except Exception:
                # 실패 시 부분이라도 누락되지 않게 그대로 전역에 추가
                gap_parts.extend(file_gap_tables)
                eul_parts.extend(file_eul_tables)
                owners_parts.extend(file_owners_tables)

        owners_df = pd.concat([d for d in owners_parts if d is not None and not d.empty and d.count().sum() > 0], ignore_index=True) if owners_parts else None
        gap_df = pd.concat([d for d in gap_parts if d is not None and not d.empty and d.count().sum() > 0], ignore_index=True) if gap_parts else None
        eul_df = pd.concat([d for d in eul_parts if d is not None and not d.empty and d.count().sum() > 0], ignore_index=True) if eul_parts else None
        

        if owners_df is not None and not owners_df.empty:
            # 안전망: 전역 병합 후에도 한 번 더 페이지 경계 병합 실행
            owners_df = self.align_columns(owners_df, self.OWNERS_TARGET_COLS)
            owners_df = self.merge_split_rows(
                owners_df,
                key_col='순위번호',
                concat_cols=[c for c in self.OWNERS_TARGET_COLS if c != '순위번호']
            )
            owners_df = self.remove_spaces_in_columns(owners_df, ["등기명의인"]).fillna('')
        if gap_df is not None and not gap_df.empty:
            # 최종 단계에서도 대상소유자 공백 제거(정규화) 적용
            gap_df = self.remove_spaces_in_columns(gap_df, ["대상소유자", "등기목적"]).fillna('')
            # 갑구 파싱 함수 적용 (원본 전체에서 지번번호 수집 후 내용기준 중복 제거)
            gap_df = self.parse_gap(gap_df)
        if eul_df is not None and not eul_df.empty:
            # 최종 단계에서도 대상소유자 공백 제거(정규화) 적용
            eul_df = self.remove_spaces_in_columns(eul_df, ["대상소유자", "등기목적"]).fillna('')
            # 을구 파싱 함수 적용 (원본 전체에서 지번번호 수집 후 내용기준 중복 제거)
            eul_df = self.parse_eul(eul_df)
            

        return owners_df, gap_df, eul_df

    def detect_duplicates(self, gap_df: pd.DataFrame, eul_df: pd.DataFrame) -> tuple[list[dict], list[dict]]:
        """전체 데이터에서 중복 탐지: 등기목적, 권리자, 금액, 대상소유자가 동일한 행들을 찾기"""
        gap_dups = []
        eul_dups = []
        
        # 갑구 중복 탐지
        if gap_df is not None and not gap_df.empty:
            try:
                dup_base = gap_df.copy()
                # 권리자/금액/대상소유자 정규화
                dup_base["_norm_권리자"] = dup_base.get("권리자/채권자/가등기권자", "").astype(str).str.replace(r"\s+", "", regex=True)
                dup_base["_norm_금액"] = dup_base.get("청구금액", "").astype(str).map(lambda x: re.sub(r"[^0-9]", "", x))
                dup_base["_norm_대상"] = dup_base.get("대상소유자", "").astype(str).str.replace(r"\s+", "", regex=True)

                # 지번 단위로 폭발(explode)하여 동일 지번 기준 중복 감지
                def _to_ids(v):
                    try:
                        if isinstance(v, (list, tuple)):
                            return [str(x).strip() for x in v if str(x).strip()]
                        return [p.strip() for p in str(v).split(',') if p.strip()]
                    except Exception:
                        return []
                if "지번일련번호" in dup_base.columns:
                    dup_base = dup_base.assign(_jid=dup_base["지번일련번호"].apply(_to_ids)).explode("_jid")
                else:
                    dup_base = dup_base.assign(_jid="")

                # 중복 그룹 찾기: 등기목적, 권리자, 금액이 동일(대상소유자는 제외)한 그룹 내에서
                # 접수정보가 다르거나 대상소유자가 다른 케이스를 탐지
                grp_cols = [c for c in ["물건번호", "등기목적", "_norm_권리자", "_norm_금액", "_jid"] if c in dup_base.columns]
                if grp_cols:
                    for _, g in dup_base.groupby(grp_cols, dropna=False):
                        if len(g) <= 1:  # 1개 행만 있으면 중복 아님
                            continue
                        
                        item_id = str(g.iloc[0].get("물건번호", ""))
                        purpose = str(g.iloc[0].get("등기목적", ""))
                        owner = str(g.iloc[0].get("권리자/채권자/가등기권자", ""))
                        amount = str(g.iloc[0].get("청구금액", ""))
                        target_owner = str(g.iloc[0].get("대상소유자", ""))
                        one_jid = str(g.iloc[0].get("_jid", ""))
                        
                        # 겹치는 행들의 순위번호/접수날짜 수집
                        def _clean_rank_text(s: str) -> str:
                            return str(s or "").replace("\u200B", "").strip()
                        def _rank_sort_key(r: str):
                            m = re.match(r"^(\d+)(?:-(\d+))?$", _clean_rank_text(r))
                            if m:
                                base = int(m.group(1))
                                sub = int(m.group(2)) if m.group(2) else 10**9
                            else:
                                base, sub = 10**9, 10**9
                            return (base, sub)
                        ranks = [_clean_rank_text(row.get("순위번호", "")) for _, row in g.iterrows()]
                        ranks = [r for r in ranks if r]
                        ranks = sorted(dict.fromkeys(ranks), key=_rank_sort_key)
                        combined_ranks = ", ".join(ranks)

                        dates = [str(row.get("접수날짜", "")).strip() for _, row in g.iterrows() if str(row.get("접수날짜", "")).strip()]
                        # 유니크 + 날짜 오름차순
                        uniq_dates = list(dict.fromkeys(dates))
                        dt_pairs = [(pd.to_datetime(s, errors="coerce"), s) for s in uniq_dates]
                        dt_pairs.sort(key=lambda t: (pd.isna(t[0]), t[0] if pd.notna(t[0]) else pd.Timestamp.max))
                        combined_dates = ", ".join([s for _, s in dt_pairs])

                        # 겹치는 행들의 접수번호(제0000호) 수집
                        rec_tokens: list[tuple[int, str]] = []
                        for _, row in g.iterrows():
                            rec = str(row.get("접수정보", "")).strip()
                            if not rec:
                                continue
                            m = re.search(r"제\s*([0-9][0-9,]*)\s*호", rec)
                            if m:
                                try:
                                    num = int(m.group(1).replace(",", ""))
                                    rec_tokens.append((num, f"제{num}호"))
                                except Exception:
                                    pass
                        rec_tokens = sorted(dict.fromkeys(rec_tokens), key=lambda x: x[0])
                        combined_receipts = ", ".join([t[1] for t in rec_tokens])

                        # 상이 정보 생성: 접수정보 또는 대상소유자 차이 여부 표시
                        try:
                            rec_keys = []
                            for _, row in g.iterrows():
                                rec = str(row.get("접수정보", ""))
                                try:
                                    key = TableProcessor._build_receipt_key(rec)
                                except Exception:
                                    key = rec
                                if key:
                                    rec_keys.append(key)
                            uniq_rec = list(dict.fromkeys([r for r in rec_keys if str(r).strip()]))
                            diff_rec = len(uniq_rec) > 1
                        except Exception:
                            diff_rec = False
                        try:
                            owners = [re.sub(r"\s+", "", str(row.get("대상소유자", ""))) for _, row in g.iterrows()]
                            uniq_owner = list(dict.fromkeys([o for o in owners if o]))
                            diff_owner = len(uniq_owner) > 1
                        except Exception:
                            diff_owner = False
                        diff_info = ", ".join([t for t in ["접수정보 상이" if diff_rec else "", "대상소유자 상이" if diff_owner else ""] if t])

                        gap_dups.append({
                                    "구분": "갑구",
                                    "물건번호": item_id,
                                    "순위번호": combined_ranks,
                                    "접수날짜": combined_dates,
                                    "접수번호": combined_receipts,
                                    "등기목적": purpose,
                                    "금액": amount,
                                    "권리자": owner,
                                    "대상소유자": target_owner,
                                    "상이 정보": diff_info,
                                })
                    
            except Exception as e:
                print(f"[WARN] 갑구 중복 수집 중 오류 발생: {e}")
        
        # 을구 중복 탐지
        if eul_df is not None and not eul_df.empty:
            try:
                dup_base = eul_df.copy()
                dup_base["_norm_권리자"] = dup_base.get("근저당권자/전세권자/채권자/지상권자/임차권자", "").astype(str).str.replace(r"\s+", "", regex=True)
                dup_base["_norm_금액"] = dup_base.get("채권최고액/전세금/임차보증금", "").astype(str).map(lambda x: re.sub(r"[^0-9]", "", x))
                dup_base["_norm_대상"] = dup_base.get("대상소유자", "").astype(str).str.replace(r"\s+", "", regex=True)

                # 지번 단위로 폭발(explode)하여 동일 지번 기준 중복 감지
                def _to_ids(v):
                    try:
                        if isinstance(v, (list, tuple)):
                            return [str(x).strip() for x in v if str(x).strip()]
                        return [p.strip() for p in str(v).split(',') if p.strip()]
                    except Exception:
                        return []
                if "지번일련번호" in dup_base.columns:
                    dup_base = dup_base.assign(_jid=dup_base["지번일련번호"].apply(_to_ids)).explode("_jid")
                else:
                    dup_base = dup_base.assign(_jid="")

                # 중복 그룹 찾기(을구): 등기목적, 권리자, 금액 동일 그룹에서 접수정보/대상소유자 상이 여부 탐지
                grp_cols = [c for c in ["물건번호", "등기목적", "_norm_권리자", "_norm_금액", "_jid"] if c in dup_base.columns]
                if grp_cols:
                    for _, g in dup_base.groupby(grp_cols, dropna=False):
                        if len(g) <= 1:  # 1개 행만 있으면 중복 아님
                            continue
                        
                        item_id = str(g.iloc[0].get("물건번호", ""))
                        purpose = str(g.iloc[0].get("등기목적", ""))
                        owner = str(g.iloc[0].get("근저당권자/전세권자/채권자/지상권자/임차권자", ""))
                        amount = str(g.iloc[0].get("채권최고액/전세금/임차보증금", ""))
                        target_owner = str(g.iloc[0].get("대상소유자", ""))
                        one_jid = str(g.iloc[0].get("_jid", ""))
                        
                        # 겹치는 행들의 순위번호/접수날짜 수집
                        def _clean_rank_text(s: str) -> str:
                            return str(s or "").replace("\u200B", "").strip()
                        def _rank_sort_key(r: str):
                            m = re.match(r"^(\d+)(?:-(\d+))?$", _clean_rank_text(r))
                            if m:
                                base = int(m.group(1))
                                sub = int(m.group(2)) if m.group(2) else 10**9
                            else:
                                base, sub = 10**9, 10**9
                            return (base, sub)
                        ranks = [_clean_rank_text(row.get("순위번호", "")) for _, row in g.iterrows()]
                        ranks = [r for r in ranks if r]
                        ranks = sorted(dict.fromkeys(ranks), key=_rank_sort_key)
                        combined_ranks = ", ".join(ranks)

                        dates = [str(row.get("접수날짜", "")).strip() for _, row in g.iterrows() if str(row.get("접수날짜", "")).strip()]
                        uniq_dates = list(dict.fromkeys(dates))
                        dt_pairs = [(pd.to_datetime(s, errors="coerce"), s) for s in uniq_dates]
                        dt_pairs.sort(key=lambda t: (pd.isna(t[0]), t[0] if pd.notna(t[0]) else pd.Timestamp.max))
                        combined_dates = ", ".join([s for _, s in dt_pairs])

                        # 겹치는 행들의 접수번호(제0000호) 수집
                        rec_tokens: list[tuple[int, str]] = []
                        for _, row in g.iterrows():
                            rec = str(row.get("접수정보", "")).strip()
                            if not rec:
                                continue
                            m = re.search(r"제\s*([0-9][0-9,]*)\s*호", rec)
                            if m:
                                try:
                                    num = int(m.group(1).replace(",", ""))
                                    rec_tokens.append((num, f"제{num}호"))
                                except Exception:
                                    pass
                        rec_tokens = sorted(dict.fromkeys(rec_tokens), key=lambda x: x[0])
                        combined_receipts = ", ".join([t[1] for t in rec_tokens])

                        # 상이 정보 생성
                        try:
                            rec_keys = []
                            for _, row in g.iterrows():
                                rec = str(row.get("접수정보", ""))
                                try:
                                    key = TableProcessor._build_receipt_key(rec)
                                except Exception:
                                    key = rec
                                if key:
                                    rec_keys.append(key)
                            uniq_rec = list(dict.fromkeys([r for r in rec_keys if str(r).strip()]))
                            diff_rec = len(uniq_rec) > 1
                        except Exception:
                            diff_rec = False
                        try:
                            owners = [re.sub(r"\s+", "", str(row.get("대상소유자", ""))) for _, row in g.iterrows()]
                            uniq_owner = list(dict.fromkeys([o for o in owners if o]))
                            diff_owner = len(uniq_owner) > 1
                        except Exception:
                            diff_owner = False
                        diff_info = ", ".join([t for t in ["접수정보 상이" if diff_rec else "", "대상소유자 상이" if diff_owner else ""] if t])

                        eul_dups.append({
                            "구분": "을구",
                            "물건번호": item_id,
                            "순위번호": combined_ranks,
                            "접수날짜": combined_dates,
                            "접수번호": combined_receipts,
                            "등기목적": purpose,
                            "금액": amount,
                            "권리자": owner,
                            "대상소유자": target_owner,
                            "상이 정보": diff_info,
                        })
                    
            except Exception as e:
                print(f"[WARN] 을구 중복 수집 중 오류 발생: {e}")
        
        # 순위번호 기반 정렬: 각 레코드의 순위번호 문자열에서 최소 (base, sub) 키로 정렬
        def _min_rank_key(rank_text: str):
            s = str(rank_text or "").replace("\u200B", "").strip()
            if not s:
                return (10**9, 10**9)
            tokens = [p.strip() for p in s.replace(',', ' ').split() if p.strip()]
            best = (10**9, 10**9)
            for tk in tokens:
                m = re.match(r"^(\d+)(?:-(\d+))?$", tk)
                if m:
                    base = int(m.group(1))
                    sub = int(m.group(2)) if m.group(2) else 10**9
                    if (base, sub) < best:
                        best = (base, sub)
            return best

        try:
            gap_dups.sort(key=lambda d: (str(d.get("물건번호", "")),) + _min_rank_key(d.get("순위번호", "")))
        except Exception:
            pass
        try:
            eul_dups.sort(key=lambda d: (str(d.get("물건번호", "")),) + _min_rank_key(d.get("순위번호", "")))
        except Exception:
            pass

        return gap_dups, eul_dups

    def aggregate_from_json_dir(self, json_dir: str) -> tuple[pd.DataFrame | None, pd.DataFrame | None, pd.DataFrame | None]:
        """디렉터리 내 모든 .json을 집계"""
        paths = glob.glob(os.path.join(json_dir, "**", "*.json"), recursive=True)
        return self.aggregate_from_json_paths(paths)

    # ----------------------- Owners 요약(물건번호별) -----------------------
    @staticmethod
    def _join_unique(values: pd.Series, sep: str = ", ") -> str:
        seen: set[str] = set()
        ordered: list[str] = []
        for v in values.astype(str).map(lambda s: s.strip()).tolist():
            if not v:
                continue
            if v not in seen:
                seen.add(v)
                ordered.append(v)
        return sep.join(ordered)

    def _convert_share_format(self, share_str: str) -> str:
        """지분 형식을 '1341분의 2'에서 '2/1341'로 변환"""
        if not share_str or share_str.strip() == "":
            return share_str
        
        share_str = str(share_str).strip()
        
        # "1341분의 2" 형태를 "2/1341"로 변환
        import re
        
        # 다양한 패턴 처리
        patterns = [
            r'(\d+(?:\.\d+)?)분의\s*(\d+(?:\.\d+)?)',  # "1341분의 2" 또는 "194819.75분 의 12089.89525"
            r'(\d+(?:\.\d+)?)분\s*의\s*(\d+(?:\.\d+)?)',  # "194819.75분 의 12089.89525"
        ]
        
        for pattern in patterns:
            match = re.search(pattern, share_str)
            if match:
                denominator = match.group(1)  # 1341 또는 194819.75
                numerator = match.group(2)    # 2 또는 12089.89525
                return f"{numerator}/{denominator}"
        
        # 이미 "2/1341" 형태인 경우 그대로 반환
        if '/' in share_str:
            return share_str
            
        return share_str

    def _get_max_share_owner_address(self, group_df: pd.DataFrame) -> str:
        """최다지분자의 주소를 반환 (동일한 최대지분이 여러 명인 경우 모두 포함)"""
        if group_df.empty:
            return ""
        
        # 주소 컬럼 확인
        if '주소' not in group_df.columns:
            return ""
        
        # 지분을 숫자로 변환하여 비교
        max_share_value = 0
        max_share_owners = []
        
        for idx, row in group_df.iterrows():
            share_str = str(row.get("최종지분", "")).strip()
            address = str(row.get("주소", "")).strip()
            
            if not share_str:
                continue
            
            # "단독소유"인 경우 지분을 1.0으로 처리
            if share_str == "단독소유":
                share_value = 1.0
            else:
                # 지분을 숫자로 변환 (분수 형태 처리)
                try:
                    if '/' in share_str:
                        # "2/1341" 형태
                        numerator, denominator = share_str.split('/')
                        share_value = float(numerator) / float(denominator)
                    elif '분' in share_str:
                        # "115분의 3.5", "194819.75분 의 12089.89525" 등 모든 분 형태 처리
                        import re
                        pattern = r'(\d+(?:\.\d+)?)분\s*의?\s*(\d+(?:\.\d+)?)'
                        match = re.search(pattern, share_str)
                        if match:
                            denominator = float(match.group(1))
                            numerator = float(match.group(2))
                            share_value = numerator / denominator
                        else:
                            continue
                    else:
                        # 단순 숫자 형태
                        share_value = float(share_str)
                    
                except (ValueError, ZeroDivisionError):
                    continue
            
            if share_value > max_share_value:
                max_share_value = share_value
                max_share_owners = [address]
            elif share_value == max_share_value:
                max_share_owners.append(address)
        
        # 중복 제거하고 빈 주소 제거
        unique_addresses = []
        seen = set()
        for addr in max_share_owners:
            if addr and addr not in seen:
                unique_addresses.append(addr)
                seen.add(addr)
        
        return ", ".join(unique_addresses)

    def _format_owners_with_shares(self, group_df: pd.DataFrame) -> str:
        """소유자 이름과 지분을 조합하여 반환 (단독소유시 이름만, 공유시 이름(지분) 형태)"""
        owner_shares = []
        for _, row in group_df.iterrows():
            name = str(row.get("등기명의인", "")).strip()
            share = str(row.get("최종지분", "")).strip()
            
            # (공유자) 제거
            name = name.replace("(공유자)", "").replace("(소유자)", "").strip()
            
            # 지분 형식은 원래대로 유지 (변환하지 않음)
            if name and share:
                owner_shares.append(f"{name}({share})")
            elif name:
                owner_shares.append(name)
        
        # 중복 제거하면서 순서 유지
        unique_owner_shares = []
        seen = set()
        for item in owner_shares:
            if item not in seen:
                unique_owner_shares.append(item)
                seen.add(item)
        
        # 단독소유인 경우 이름만, 공유인 경우 이름(지분) 형태로 반환
        if len(group_df) == 1:
            # 단독소유: 이름만 반환
            names_only = []
            for _, row in group_df.iterrows():
                name = str(row.get("등기명의인", "")).strip()
                name = name.replace("(공유자)", "").replace("(소유자)", "").strip()
                if name:
                    names_only.append(name)
            result = ", ".join(names_only)
        else:
            # 공유: 이름(지분) 형태로 반환
            result = ", ".join(unique_owner_shares)
        
        return result

    def summarize_owners(self, owners_df: pd.DataFrame) -> pd.DataFrame:
        """
        owners_df를 지번일련번호 기준으로 요약하여 다음 컬럼으로 반환:
        - 소유자: '등기명의인'을 ", "로 병합
        - 등록번호: '(주민)등록번호'를 ", "로 병합
        - 최종지분: '최종지분'을 ", "로 병합
        - 소유자 주소: '주소' 또는 '주 소'를 ", "로 병합
        """
        if owners_df is None or owners_df.empty:
            return pd.DataFrame(columns=["지번일련번호", "소유자", "등록번호", "최종지분", "소유자 주소"])  # 빈 형태

        df = owners_df.copy()
        # 주소 컬럼명 통일
        if "주소" not in df.columns and "주 소" in df.columns:
            df = df.rename(columns={"주 소": "주소"})
        if "주소" not in df.columns:
            df["주소"] = ""

        for col in ["등기명의인", "(주민)등록번호", "최종지분", "주소", "지번일련번호"]:
            if col not in df.columns:
                df[col] = ""

        groups = []
        for jibeon_id, g in df.groupby("지번일련번호", dropna=False):
            row = {
                "지번일련번호": str(jibeon_id).strip(),
                "소유자": self._format_owners_with_shares(g),
                "등록번호": self._join_unique(g["(주민)등록번호"]),
                "최종지분": "단독소유" if len(g) == 1 else f"{len(g)}명 소유",
                "소유자 주소": self._get_max_share_owner_address(g),
            }
            groups.append(row)
        
        return pd.DataFrame(groups)