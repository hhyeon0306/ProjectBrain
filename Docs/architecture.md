# Project Brain 데이터 및 구조 설계

2026-09-06 검토 기준 구현: `7fd9d66`. 이 문서는 데이터 계약의 기준이다. **구현됨 / 계획 / 미결정**을 구분한다. 제품 목표는 [product_spec.md](product_spec.md), 실행 순서는 [task.md](task.md)를 따른다.

## 구현된 A1 저장 형식

```text
.projectbrain/
├─ docs/<Unity GUID>.json       기존 v1/v2 문서, 보존·기존 UI 사용
├─ nodes/<SHA256(node ID)>.json 범용 노드 schemaVersion=1
├─ relations.json              관계 배열과 schemaVersion=1
└─ migration.json              원본 버전·해시와 이관 결과 ID 목록
```

docs의 v2와 새 노드의 v1은 별개의 스키마다. 실제 데이터는 Code 7개, Document 7개, Image 1개, 관계 26개다. 나머지 종류도 저장 가능하지만 실제 도메인·검증·활동 기록은 아직 생성하지 않았다.

### BrainNode

| 필드 | 현재 동작 |
|---|---|
| schemaVersion | 명시된 버전은 1만 허용. 누락 시 JsonUtility가 초기값 1로 채우는 결함 확인, A1-R 보완 필요 |
| id / type | 아래 9종과 ID 규칙. 동일 ID의 type/assetGuid 변경 거절 |
| title | 공백만 있는 제목 거절 |
| summary / body | null이 아닌 문자열. 역할·설계 의도·주의사항 전용 필드는 없음 |
| assetGuid | Code/Image에 필수. 소문자 32자리 GUID와 asset ID 일치 검사 |
| lastKnownPath | 호출자가 제공. 절대 경로·역슬래시·상위 이동 거절. 실제 자산 존재 검사는 아님 |
| tags | null 또는 빈 항목 거절. 검색 인덱스 미구현 |
| status | unreviewed / missing / recorded. 성공이나 사람 확인을 의미하지 않음 |
| updatedUtc | Z로 끝나는 UTC 시각을 호출자가 제공. SaveNode는 자동 갱신하지 않음 |

| 종류 | 역할 | ID |
|---|---|---|
| Project | 프로젝트 | `project:<slug>` |
| Domain | Player 등 영역 | `domain:<slug>` |
| Feature | Movement 등 기능 | `feature:<domain>/<slug>` |
| Code | 코드 자산 | `asset:<GUID>` |
| Document | 설계·운영 설명 | `document:<slug 또는 UUID>` |
| Image | 이미지 자산 | `asset:<GUID>` |
| Evidence | 검증 기록 탐색 노드 | `evidence:<UUID>` |
| Activity | 작업 이력 탐색 노드 | `activity:<UUID>` |
| Reference | 참고 자료 | `reference:<slug 또는 UUID>` |

Artifact는 Code/Document/Image를 묶는 설명용 단어이며 실제 type 값이 아니다. ID는 소문자 영문·숫자 기반, 제목·본문은 한글 사용 가능하다. Feature는 domain/slug 형식, Evidence/Activity는 하이픈 포함 UUID 형식을 검사한다. ID에 domain이 있어도 부모 노드의 존재를 보장하지는 않는다.

Evidence/Activity의 동일 ID 재저장은 API가 거절한다. 로컬 파일 직접 수정까지 차단하는 보안 기능은 아니다. 기록의 진위 검증과 외부 변경 감시는 아직 없다.

### BrainRelation

필드: id, from, to, type, source, createdUtc. 개별 관계에는 버전 필드가 없고 relations.json 래퍼가 버전을 가진다.

| type | 목표 의미·방향 예시 |
|---|---|
| contains | Project → Domain → Feature |
| depends_on | Movement → Input |
| implemented_by | Feature → Code |
| documented_by | Feature 또는 Code → Document |
| illustrated_by | Document → Image |
| verified_by | Feature 또는 Code → Evidence |
| worked_on_in | Feature 또는 Code → Activity |
| references | Feature 또는 Document → Reference |

현재 검사: ID 형식, 관계 ID 및 from/type/to 조합의 중복, 허용 type, 출처·UTC 시각, 양쪽 노드 존재. RelationsFor(id)는 방향을 유지해 양방향 관계를 반환한다. 관계 파일 손상 또는 참조 노드 누락 시 관계 조회·저장을 거절한다.

**미구현:** 관계 종류별 대상 type, contains 순환·자기 참조·여러 부모, Feature ID의 domain과 부모 일치. 노드 목록 조회 자체는 전체 관계 무결성 검사가 아니다. A2의 계층 서비스·렌더러는 잘못된 계층을 오류로 표시하고 순환 방문을 방지해야 한다. depends_on을 contains 트리와 분리하며 모든 관계에 비순환 조건을 적용하지 않는다.

### 저장 보장과 한계

- 파일명은 전체 ID의 UTF-8 SHA256 소문자 hex다. 표시·탐색에는 id/title을 사용한다.
- 각 노드 파일 및 relations.json은 임시 파일 후 교체한다. 여러 노드·관계 전체의 트랜잭션은 아니다.
- JSON 구문 오류·필수 필드 누락·잘못된 필드 형식·중복 키·미지원 버전의 API 덮어쓰기를 거절한다. UnityBrainJson은 기존 System.Text.Json 8 DLL로 입력 구조를 검사한 뒤 JsonUtility로 읽는다(새 설치 없음). 노드의 assetGuid/lastKnownPath를 제외한 필드, 관계 파일/항목 및 이관 기록의 필드를 요구한다. 레거시 문서는 schemaVersion/scriptGuid만 필수이며 나머지 필드의 누락/null과 명시적 v1 호환은 유지한다. 알 수 없는 추가 필드는 무시하며 자동 복구·삭제 API는 없다.
- 단일 Editor 작성자를 전제로 하며 잠금·예상 버전 비교·동시 쓰기 제어는 없다.
- GUID 형식과 실제 자산 종류·존재는 별개다. 후자는 Unity 어댑터 책임이다.

## v2 비파괴 이관: 구현됨

BrainMigration.Run은 DocumentStore로 원본 전체를 검증하고 Code/Document/Image 노드와 관계를 준비한다. v1 문서도 기존 읽기 경로를 통해 v2 형태로 읽는다. 원본 bytes는 수정하지 않는다.

- Code summary에 role을 담는다. Document body에는 역할·설계 의도·주의사항·본문·과거 코드 해시를 제목이 있는 텍스트로 보존한다. 구조화된 필드나 현재 검증 증거가 되는 것은 아니다.
- Code → Document는 documented_by, Document → Image는 illustrated_by다.
- relatedScriptGuids는 source=v2-migration인 임시 depends_on이다. 원래 수동 관련성 데이터이며 코드 의존성 분석 결과가 아니다.
- 주입된 GUID 해석 함수로 경로를 찾는다. 없는 자산은 missing으로 남긴다. missing은 이관 시점 결과다.
- 노드 시각은 원본 updatedUtc다. 없으면 결정적 재실행을 위해 1970-01-01T00:00:00Z를 사용한다. 이관 실행 시각은 receipt의 completedUtc다.
- 충돌 사전 검사 → 없는 노드 저장 → 관계 저장 → 재읽기 → 원본 변경 확인 → receipt 순서다. 부분 결과가 남을 수 있으며 동일 원본·자산 해석과 정확히 일치하는 부분 결과에서 재실행한다.

receipt는 schemaVersion=1, completedUtc, sources(GUID/원본 버전/SHA256), nodeIds, relationIds를 가진다. 완료 후 재실행은 원본 일치와 결과 ID 존재를 검사하고 목적지의 후속 편집을 보존한다. 목적지 내용이 최초 이관 결과와 같다는 검사는 아니다. 원본 변경은 거절하며 자동 병합·재조정 기능은 없다. receipt 삭제로 우회하지 않는다.

**A1-R 보완 완료:** `.gitattributes`의 `.projectbrain/** -text`로 저장 데이터의 Git 줄바꿈 변환을 금지한다. 원본 SHA256은 계속 정확한 bytes 기준이며 정규화·receipt 재작성으로 변경을 무시하지 않는다. 기존 receipt와 원본은 수정하지 않았다. core.autocrlf=true인 새 clone에서 이관 2회와 전체 데이터 bytes 보존을 확인했다. 이미 줄바꿈이 변환된 과거 사본은 자동 복구하지 않는다. 새 clone의 Unity import/빌드는 이번 확인 범위가 아니다.

## A2에서 확정·구현할 사용 계약

1. 새 계층 UI의 기준은 nodes/relations다. 기존 Script Document 창은 docs를 편집한다. 새 UI 도입 시 기존 창의 안내·읽기 모드 또는 명시적 분리를 정해 두 저장소를 같은 데이터처럼 보이지 않게 한다.
2. Player와 소수 Feature를 명시적으로 구성한다. 기존 문서 7개 전체를 Player 소속으로 자동 편입하지 않는다.
3. 처음에는 계층 탐색과 자료 열기를 완성한다. 증거가 없으면 미검증으로 표시한다. 임시 depends_on 출처를 표시하고, 관계 편집을 제공할 때도 공통 서비스를 사용한다.
4. 역할·설계 의도·주의사항을 독립 편집하려면 명시적 필드나 별도 Document payload 계약이 필요하다. 임의 Markdown 제목 파싱을 장기 계약으로 삼지 않는다. 형식 변경 시 스키마와 비파괴 이관을 먼저 정한다.
5. contains의 허용 부모·자식, 순환 방지, 노드 이동 시 ID 정책은 구현 전에 확정한다. 이는 A1이 이미 보장한 조건이 아니다.

## 작업·검증·활동 기록: 계획, 미구현

### W1/M2a 최소 계약 확정 (2026-09-06, 구현 전)

아래는 다음 구현의 수용 계약이다. 저장 모델·서비스·MCP가 이미 존재한다는 뜻은 아니다.

- 작업 저장: tasks/active.json에 schemaVersion, UUID 작업 ID, revision, 목적, 대상 노드 ID, 진행 요약, 결정/근거, 미해결, 다음 행동, 원본 참조, 시작 스냅샷, createdUtc/updatedUtc를 저장한다. 필수 필드 누락·손상은 거절하고 기존 파일을 보존한다. 저장은 파일 단위 원자 교체이며 단일 Editor 작성자를 전제로 한다.
- begin/restart: 활성 작업이 없을 때만 새 기준선을 만든다. 활성 작업이 있으면 그 ID와 요약을 반환하며 새 목적/대상으로 덮어쓰지 않는다. 명시한 taskId가 활성 ID와 다르면 충돌로 거절한다. 재개는 기존 기준선과 요약을 유지하고 현재 변경 목록을 다시 계산한다. 새 작업으로 교체·완료하는 정책은 W2에서 구현한다.
- 요약 갱신: UI와 MCP가 같은 update-task 서비스에 taskId와 expectedRevision을 전달한다. revision 불일치 또는 잘못된 참조는 쓰기 전에 거절한다. 요약 저장은 기준선·확인·검증 결과를 갱신하지 않는다. MCP 어댑터 이름은 brain_update_task로 두고 문서 편집과 구분한다.
- 감시: 프로젝트 내부 Assets/Packages/ProjectSettings의 실제 파일과 meta를 경로별 SHA256 bytes로 읽는다. Library/Temp/Logs/.git과 Brain 작업 기록은 제외한다. 외부 file: 패키지·심볼릭 링크 등 완전히 읽지 못하는 입력은 coverage 제한으로 반환하고 완료 근거로 삼지 않는다. 허용 변경 경로는 감시 범위 안의 별도 목록이다. 범위 밖 변경도 반환한다. 이름 변경은 삭제+추가로 표시하며 GUID로 추정한 이동은 별도 힌트다. begin 이전 변경은 기준선에 포함되므로 이후 변경과 혼동하지 않는다. Git의 기존 dirty 여부는 별도 참고 정보다.
- context 입력: rootNodeId, depth(기본 1, 0~2), maxNodes(기본 12, 1~30), maxChars(기본 8000, 1000~20000)를 받는다. 관계는 양방향 BFS로 탐색하되 from/to/type/source를 보존한다. 같은 깊이에서는 contains, implemented_by, documented_by, illustrated_by, depends_on, verified_by, worked_on_in, references 순서 후 관계 ID의 ordinal 순서로 고정한다. 방문 집합으로 순환·중복 확장을 막는다.
- context 출력: 선택 노드·요약·원본 참조·관계·최신성 근거와 조회량을 반환한다. 긴 body, 이미지 bytes, 로그 원문은 자동 포함하지 않는다. maxChars는 직렬화된 응답의 UTF-16 코드 단위 수로 계산하며 메타데이터도 예산에 포함한다. 생략 이유(depth/nodes/chars), 생략 수(계산한 범위만), 후속 조회용 노드 ID를 예산 내에서 반환한다. 루트의 최소 식별 정보도 맞지 않으면 budget-too-small로 거절한다. 전체 그래프를 세지 않고 미탐색 수를 추정 수치로 꾸미지 않는다.
- 최신성: 설명/관계의 근거는 별도 freshness 기록에 대상 ID, 설명 또는 관계 payload 해시, 근거 코드 ID/경로/SHA256 집합으로 저장한다. 자료 내용과 근거 코드 모두 일치하면 current, 달라지면 stale, 근거가 없으면 unknown, 참조 파일이 없으면 missing이다. 이관된 자료는 명시적 근거를 등록하기 전 unknown이다. current는 bytes 일치만 뜻하며 정확성·사람 확인·검증 성공을 대신하지 않는다. 조회 시 다시 계산하며 재개만으로 근거를 새 코드에 맞춰 덮어쓰지 않는다.

회귀 수용 사례: 새 서비스 인스턴스에서 동일 작업/기준선 재개, 다른 begin의 덮어쓰기 거절, revision 충돌의 원본 보존, 순환 그래프의 결정적 제한 조회, 응답 예산/생략 표시, 코드·설명·관계 변경 후 stale, 근거 없음 unknown, 파일 삭제 missing. 실제 구현 결과와 조회량은 이후 실행 증거로 남긴다.

BrainTask는 ID, 목적, 대상 Feature/노드, 감시 범위, 허용 변경 범위, 시작 스냅샷, 변경 목록, 상태를 가진다. 활성 작업은 하나다. 기존 사용자 변경과 begin 이후 변경을 구분한다. 추가로 진행 요약·결정/근거·미해결·다음 행동을 지속 저장해 세션 재개에 사용한다. 정확한 스키마와 갱신/재개 API는 W1에서 확정한다.

M2a 착수 시 관계별 탐색 방향·깊이·우선순위·분량 예산과 생략/후속 조회 응답을 정한다. 반환 자료는 노드 ID·관계 출처·최신성 근거를 포함하도록 설계한다. 설명/관계의 기준 코드 해시와 변경 후 재확인 상태를 어떻게 저장할지는 W1/M2a에서 확정하며, 기존 updatedUtc나 missing만으로 최신성을 보장하지 않는다. UI 전체 완성은 공통 맥락 서비스의 선행 조건이 아니다.

VerificationRecord는 실행 ID, 작업 ID, 종류(compile/editmode/playmode), 검증 범위·대상 해시 집합, 시작·종료 시각, 결과·실행/실패/무시 수, 요약·로그 위치를 가진다. 실행기가 생성하며 recorded 상태나 body의 성공 문구로 대체하지 않는다.

문서 확인에는 문서 ID·내용 해시, 대상 코드 스냅샷, 확인 주체·시각이 필요하다. 코드 또는 문서 변경 시 무효화한다. 사람 확인이 필요한 경우 Unity의 명시적 확인 경로를 구현하고 MCP의 AI 작성과 구분한다. 인증·변조 방지를 보장하는 것은 아니다.

Activity는 제품의 작업 이력이며 Docs/work_log.md는 Brain 자체 개발 이력이다. 현재는 Activity 종류의 노드 저장만 가능하다.

예정 위치는 tasks/active.json, evidence/<verification-id>.json, activities/<activity-id>.json이다. **노드와 상세 기록의 연결 방식·원본 책임은 A3/V1 착수 때 확정한다.** 현재 해당 상세 모델·디렉터리는 미구현이다.

## 완료 조건 구현 전 결정할 사항

- 감시 범위와 허용 수정 범위는 별개다. 허용 범위만 스캔하면 범위 밖 변경을 못 찾는다. 추가·수정·삭제·이름 변경·meta 처리 및 제외 경로를 W1에서 명시한다.
- 완료 시 스냅샷을 다시 읽는다. 파일 이벤트만으로 무효화가 충분하다고 가정하지 않는다.
- 검증 중·후 코드 변경, 문서 변경, 테스트 0개, 실패·시간 초과·취소·재컴파일 중을 성공과 구분한다.
- 필수 문서·검증 정책과 거절 이유·다음 행동 구조를 W2에서 정의한다. AI가 정책을 낮추거나 사람 확인을 대신해 완료하지 않게 한다.
- 장시간 verify는 실행 ID와 후속 상태 조회를 사용한다. 재시작한 진행 중 작업을 성공 처리하지 않는다.

## 구현 경계

BrainNode/BrainStore는 UI·MCP·Unity 타입에 직접 의존하지 않고 IBrainJson을 받는다. UnityBrainJson은 JsonUtility를 사용한다. **기존 DocumentStore도 JsonUtility를 쓰며 BrainMigration은 이 경로에 의존한다.** 현재 모든 코드는 하나의 Editor asmdef에 있다. 별도 Core 어셈블리나 Unity 외 실행 검증을 완료한 것은 아니다.

Unity 어댑터는 자산 해석, 컴파일·테스트, 메인 스레드를 담당한다. UI와 기존 Unity-MCP에 추가할 Brain 도구는 같은 서비스로 상태를 변경한다. 기존 Unity-MCP/PackageCache를 수정하거나 별도 서버·제품 CLI를 만들지 않는다.

## 기반 코드 검토에서 추가 확인

2026-09-06 [프로젝트 검토](reviews/2026-09-06-project-review.md)에서 빈 관계 객체/누락 schemaVersion 수용과 덮어쓰기, 새 Git clone의 이관 거절을 재현했다. 문서 명세의 목표와 현재 유효성 검사가 다르므로 수정 전까지 일반적인 손상 데이터 보호 또는 클린 체크아웃 재현성을 완료했다고 주장하지 않는다.
