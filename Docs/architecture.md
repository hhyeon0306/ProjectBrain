# Project Brain 데이터 및 구조 설계

2026-09-06 현재 A1/A1-R 및 A2/W1/M2a 최소 구현 기준. 이 문서는 데이터 계약의 기준이다. **구현됨 / 계획 / 미결정**을 구분한다. 제품 목표는 [product_spec.md](product_spec.md), 실행 순서는 [task.md](task.md)를 따른다.

## 구현된 A1 저장 형식

```text
.projectbrain/
├─ docs/<Unity GUID>.json       기존 v1/v2 문서, 보존·기존 UI 사용
├─ nodes/<SHA256(node ID)>.json 범용 노드 schemaVersion=1
├─ relations.json              관계 배열과 schemaVersion=1
└─ migration.json              원본 버전·해시와 이관 결과 ID 목록
```

docs의 v2와 새 노드의 v1은 별개의 스키마다. 실제 데이터는 Project 1·Domain 1·Feature 2·Code 7·Document 7·Image 1개(19노드), 관계 32개다. Evidence·Activity 상세 기록은 아직 없다.

### BrainNode

| 필드 | 현재 동작 |
|---|---|
| schemaVersion | 명시된 정수 버전 1만 허용. 누락/잘못된 형식은 A1-R 입력 검사에서 거절 |
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

**A2 공통 서비스:** contains는 Project→Domain→Feature만 허용하고 복수 부모·Feature ID/Domain 불일치를 거절한다. 이 종류 제약으로 contains 순환·자기 참조도 거절한다. implemented_by는 Feature→Code, documented_by는 Feature/Code→Document, illustrated_by는 Document→Image를 검사한다. 나머지 의미 관계의 대상 제약은 후속이다. BrainStore 자체는 범용 저장소이고 BrainGraphService를 사용하는 UI/MCP가 계층을 검사한다. depends_on 순환은 허용하며 context는 방문 집합으로 제한한다.

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

## A2 최소 사용 계약과 후속 편집 범위

1. 새 계층 UI의 기준은 nodes/relations다. 기존 Script Document 창은 docs를 편집한다. 새 UI 도입 시 기존 창의 안내·읽기 모드 또는 명시적 분리를 정해 두 저장소를 같은 데이터처럼 보이지 않게 한다.
2. Player와 소수 Feature를 명시적으로 구성한다. 기존 문서 7개 전체를 Player 소속으로 자동 편입하지 않는다.
3. 처음에는 계층 탐색과 자료 열기를 완성한다. 증거가 없으면 미검증으로 표시한다. 임시 depends_on 출처를 표시하고, 관계 편집을 제공할 때도 공통 서비스를 사용한다.
4. 역할·설계 의도·주의사항을 독립 편집하려면 명시적 필드나 별도 Document payload 계약이 필요하다. 임의 Markdown 제목 파싱을 장기 계약으로 삼지 않는다. 형식 변경 시 스키마와 비파괴 이관을 먼저 정한다.
5. contains 허용 계층·복수 부모·Feature ID 부모 일치는 공통 서비스에서 검사한다. 노드 이동/관계 편집 UI는 이번 읽기 탐색 범위 밖이며 ID 변경 정책은 편집 기능 때 확정한다.

## 작업·맥락: 최소 구현됨 / 검증·활동: 계획

### W1/M2a 최소 계약 (2026-09-06 구현·검증)

아래 계약의 작업 저장·재개·맥락·최신성은 구현됐다. 실제 호출과 제한은 [brain-usage.md](brain-usage.md)를 따른다. 완료 승인과 실제 검증 결과 연결은 W2/V1 후속이다.

- 작업 저장: tasks/active.json에 schemaVersion, UUID 작업 ID, revision, 목적, 대상 노드 ID, 진행 요약, 결정/근거, 미해결, 다음 행동, 원본 참조, 시작 스냅샷, createdUtc/updatedUtc를 저장한다. 필수 필드 누락·손상은 거절하고 기존 파일을 보존한다. 저장은 파일 단위 원자 교체이며 단일 Editor 작성자를 전제로 한다.
- begin/restart: 활성 작업이 없을 때만 새 기준선을 만든다. 활성 작업이 있으면 그 ID와 요약을 반환하며 새 목적/대상으로 덮어쓰지 않는다. 명시한 taskId가 활성 ID와 다르면 충돌로 거절한다. 재개는 기존 기준선과 요약을 유지하고 현재 변경 목록을 다시 계산한다. 새 작업으로 교체·완료하는 정책은 W2에서 구현한다.
- 요약 갱신: UI와 MCP가 같은 update-task 서비스에 taskId와 expectedRevision을 전달한다. revision 불일치 또는 잘못된 참조는 쓰기 전에 거절한다. 요약 저장은 기준선·확인·검증 결과를 갱신하지 않는다. MCP 어댑터 이름은 brain_update_task로 두고 문서 편집과 구분한다.
- 감시: 프로젝트 내부 Assets/Packages/ProjectSettings의 실제 파일과 meta를 경로별 SHA256 bytes로 읽는다. Library/Temp/Logs/.git과 Brain 작업 기록은 제외한다. 외부 file: 패키지·심볼릭 링크 등 완전히 읽지 못하는 입력은 coverage 제한으로 반환하고 완료 근거로 삼지 않는다. 허용 변경 경로는 감시 범위 안의 별도 목록이다. 범위 밖 변경도 반환한다. 이름 변경은 삭제+추가로 표시하며 GUID로 추정한 이동은 별도 힌트다. begin 이전 변경은 기준선에 포함되므로 이후 변경과 혼동하지 않는다. Git dirty는 현재 자동 조회하지 않고 not-queried로 명시하며 세션의 Git 상태 확인과 구분한다. GUID 이동 추정은 아직 없으며 삭제+추가만 반환한다.
- context 입력: rootNodeId, depth(기본 1, 0~2), maxNodes(기본 12, 1~30), maxChars(기본 8000, 1000~20000)를 받는다. 관계는 양방향 BFS로 탐색하되 from/to/type/source를 보존한다. 같은 깊이에서는 contains, implemented_by, documented_by, illustrated_by, depends_on, verified_by, worked_on_in, references 순서 후 관계 ID의 ordinal 순서로 고정한다. 방문 집합으로 순환·중복 확장을 막는다.
- context 출력: 선택 노드·요약·원본 참조·관계·최신성 근거와 조회량을 반환한다. 긴 body, 이미지 bytes, 로그 원문은 자동 포함하지 않는다. maxChars는 실제 compact JSON의 UTF-16 코드 단위 수로 계산하며 Unicode escape, 메타데이터 및 Ivan의 result 래퍼 11자도 포함한다. MCP 프로토콜 외부 봉투는 제외한다. DTO 반환으로 JSON 문자열의 이중 인코딩을 피한다. 생략 이유(depth/nodes/chars), 생략 수(계산한 범위만), 후속 조회용 노드 ID를 예산 내에서 반환한다. 루트의 최소 식별 정보도 맞지 않으면 budget-too-small로 거절한다. 전체 그래프를 세지 않고 미탐색 수를 추정 수치로 꾸미지 않는다.
- 최신성: 설명/관계의 근거는 별도 freshness 기록에 대상 ID, 설명 또는 관계 payload 해시, 근거 코드 ID/경로/SHA256 집합으로 저장한다. 자료 내용과 근거 코드 모두 일치하면 current, 달라지면 stale, 근거가 없으면 unknown, 참조 파일이 없으면 missing이다. 이관된 자료는 명시적 근거를 등록하기 전 unknown이다. current는 bytes 일치만 뜻하며 정확성·사람 확인·검증 성공을 대신하지 않는다. 조회 시 다시 계산하며 재개만으로 근거를 새 코드에 맞춰 덮어쓰지 않는다.

회귀 수용 사례: 새 서비스 인스턴스에서 동일 작업/기준선 재개, 다른 begin의 덮어쓰기 거절, revision 충돌의 원본 보존, 순환 그래프의 결정적 제한 조회, 응답 예산/생략 표시, 코드·설명·관계 변경 후 stale, 근거 없음 unknown, 파일 삭제 missing. 실제 구현 결과와 조회량은 reviews/2026-09-06-workflow-evidence.json에 기록했다. 동일 깊이 전체의 관계 종류/ID 정렬 회귀를 포함한다.

BrainTask는 ID, 목적, 대상 Feature/노드, 감시 범위, 허용 변경 범위, 시작 스냅샷, 변경 목록, 상태를 가진다. 활성 작업은 하나다. 기존 사용자 변경과 begin 이후 변경을 구분한다. 추가로 진행 요약·결정/근거·미해결·다음 행동을 지속 저장해 세션 재개에 사용한다. 현재 BrainTaskRecord와 BrainTaskService에 저장·revision 갱신·재개 계약을 구현했다. 상태는 active 하나이며 완료 거절 검사는 W2에 구현했고 성공/새 작업 교체는 후속이다.

M2a의 탐색 방향·깊이·순서·예산·관측된 생략/후속 ID는 위 계약으로 구현했다. 반환 자료는 노드 ID·관계 출처·최신성 근거를 포함하도록 설계한다. freshness/<SHA256(target ID)>.json에 payload 해시와 코드 ID/현재 경로/해시를 별도로 저장한다. 명시적 brain_record_basis는 바이트 기준선이며 사람 확인이 아니다. UI 전체 완성은 공통 맥락 서비스의 선행 조건이 아니다.

VerificationRecord는 실행 ID, 작업 ID, 종류(compile/editmode/playmode), 검증 범위·대상 해시 집합, 시작·종료 시각, 결과·실행/실패/무시 수, 요약·로그 위치를 가진다. 실행기가 생성하며 recorded 상태나 body의 성공 문구로 대체하지 않는다.

문서 확인에는 문서 ID·내용 해시, 대상 코드 스냅샷, 확인 주체·시각이 필요하다. 코드 또는 문서 변경 시 무효화한다. 사람 확인이 필요한 경우 Unity의 명시적 확인 경로를 구현하고 MCP의 AI 작성과 구분한다. 인증·변조 방지를 보장하는 것은 아니다.

Activity는 제품의 작업 이력이며 Docs/work_log.md는 Brain 자체 개발 이력이다. 현재는 Activity 종류의 노드 저장만 가능하다.

예정 위치는 tasks/active.json, evidence/<verification-id>.json, activities/<activity-id>.json이다. **노드와 상세 기록의 연결 방식·원본 책임은 A3/V1 착수 때 확정한다.** 현재 tasks/active.json 및 freshness 기록은 구현됐다. evidence/activities 상세 모델·디렉터리는 미구현이다.

## 완료 조건 구현 전 결정할 사항

- 감시 범위와 허용 수정 범위는 별개다. 허용 범위만 스캔하면 범위 밖 변경을 못 찾는다. 추가·수정·삭제·이름 변경·meta 처리 및 제외 경로를 W1에서 명시한다.
- 완료 시 스냅샷을 다시 읽는다. 파일 이벤트만으로 무효화가 충분하다고 가정하지 않는다.
- 검증 중·후 코드 변경, 문서 변경, 테스트 0개, 실패·시간 초과·취소·재컴파일 중을 성공과 구분한다.
- 필수 문서·검증 정책과 거절 이유·다음 행동 구조를 W2에서 정의한다. AI가 정책을 낮추거나 사람 확인을 대신해 완료하지 않게 한다.
- 장시간 verify는 실행 ID와 후속 상태 조회를 사용한다. 재시작한 진행 중 작업을 성공 처리하지 않는다.

## 구현 경계

BrainNode/BrainStore는 UI·MCP·Unity 타입에 직접 의존하지 않고 IBrainJson을 받는다. UnityBrainJson은 JsonUtility를 사용한다. **기존 DocumentStore는 UnityBrainJson 입력 검사 후 JsonUtility를 쓰며 BrainMigration은 이 경로에 의존한다.** 현재 모든 코드는 하나의 Editor asmdef에 있다. 별도 Core 어셈블리나 Unity 외 실행 검증을 완료한 것은 아니다.

Unity 어댑터는 자산 해석, 컴파일·테스트, 메인 스레드를 담당한다. UI와 기존 Unity-MCP에 추가할 Brain 도구는 같은 서비스로 상태를 변경한다. 기존 Unity-MCP/PackageCache를 수정하거나 별도 서버·제품 CLI를 만들지 않는다.

## 기반 코드 검토에서 추가 확인

2026-09-06 [프로젝트 검토](reviews/2026-09-06-project-review.md)에서 빈 관계 객체/누락 schemaVersion 수용과 덮어쓰기, 새 Git clone의 이관 거절을 재현했다. 문서 명세의 목표와 현재 유효성 검사가 다르므로 수정 전까지 일반적인 손상 데이터 보호 또는 클린 체크아웃 재현성을 완료했다고 주장하지 않는다.

## W2/M1 구현 계약 (2026-09-07)

고정 정책 w2-human-review-v1-pending: 작업 대상의 contains/implemented_by 하위와 감시 변경에 매핑된 Code, 그 소유 Feature 및 형제 Code를 검사한다. Code마다 직접 documented_by 문서가 1개 이상 필요하며 연결된 문서는 모두 필수다. Feature 직접 문서도 포함한다. 연결 없는 Code/문서, 미매핑 변경, 허용 밖 변경, coverage 제한은 거절한다. 삭제 파일은 lastKnownPath도 매핑에 사용한다. 자동 의존 분석은 아니다.

확인 기록은 .projectbrain/reviews/SHA256(taskId + 개행 + documentId).json이다. schemaVersion/taskId/documentId/snapshotHash/actor=human-ui/reviewedUtc를 저장한다. snapshotHash는 문서 전체 payload, 정렬된 전체 관계, 연결 Code 노드와 파일 경로/bytes를 포함한다. 전체 관계 변경도 보수적으로 stale 처리한다. 바이트를 정확히 되돌리면 current로 복원된다. missing-code/unreviewed/current/stale을 구분하며 손상 기록은 덮어쓰지 않는다.

Explorer에서 현재 본문과 연결 코드를 읽었다는 체크 후 사람 확인 기록 버튼을 누른다. 화면 문서가 디스크와 다르거나 작업 ID/revision·확인 대상 해시가 바뀌면 거절한다. AI basis 저장은 사람 확인이 아니며 MCP에 승인/정책 낮추기 입력은 없다. 이 경로는 사용자 확인 진술 기록이며 인증·외부 파일 변조 방지 보안 경계는 아니다. 실제 프로젝트의 사람 확인은 이번에 수행하지 않았다.

brain_status.completion은 고정 정책 판정을 포함한다. brain_complete(taskId, expectedRevision)는 현재 파일을 다시 검사하여 completed=false, documents, reasons(code/target/nextAction)를 반환한다. V1 미구현이므로 verification-unavailable은 항상 포함하고 작업/기준선/상태를 변경하지 않는다. begin 응답의 completion은 null이며 status에서 조회한다. 다중 파일 원자 스냅샷/동시 편집 보장은 없다. 실제 검증 성공·작업 교체·archive는 후속이다.

## A3/V1 검증 계약 (2026-09-07)

brain_verify는 taskId/revision과 compile 또는 editmode를 받아 실행 ID를 즉시 반환한다. compile은 Unity CompilationPipeline 이벤트, editmode는 기존 Ivan Tool_Tests.TestRunnerApi의 전체 EditMode 실행/종료 콜백을 사용한다. 별도 서버/CLI나 호출자 입력 성공 결과는 사용하지 않는다. 미저장 씬·컴파일/플레이·다른 테스트 실행 중에는 시작하지 않는다. 설치된 TestRunner의 internal IsRunActive를 읽어 동시 실행을 거절하며 해당 API가 없으면 시작을 거절한다.

검증 스냅샷은 Assets/Packages/ProjectSettings의 파일 내용과 의미 노드/관계다. 자동 생성 Evidence/Activity 및 verified_by/worked_on_in은 스냅샷에서 제외해 결과를 저장했다는 이유로 결과 자체가 무효화되지 않게 한다. 코드/설명/의미 관계 변경은 무효화한다. 기록은 evidence/<runId>.json, 탐색 노드의 body는 해당 상세 파일을 참조한다. 결과 노드 status=recorded는 통과를 뜻하지 않는다.

최소 정책은 컴파일과 전체 발견 EditMode 테스트다. 실패·0개·건너뜀·미확정·재시작 중단·5분 초과·스냅샷 변경은 통과하지 않는다. 모든 게임 기능의 테스트 커버리지나 PlayMode/Player 빌드 성공을 뜻하지 않는다.

완료 요청은 정책을 만족하면 activities/<UUID>.json에 작업 ID/revision·스냅샷·검증 실행 ID·완료 시각을 기록하고 Activity 노드/관계로 연결한다. status.ready는 현재 조건 충족 여부이고 completed는 실제 complete 호출의 기록 성공 여부다. 활성 작업 기준선은 유지하며 작업 자동 교체/아카이브는 별도 후속이다. 같은 작업의 나중 변경은 예전 Activity를 지우지 않지만 현재 완료 조건은 다시 검사한다. 여러 파일의 트랜잭션 보장은 없으며 부분 발행 실패 시 완료 성공을 반환하지 않는다.

컴파일 결과의 total/passed는 어셈블리 단위이며 테스트 개수가 아니다. summary에 compiled와 up-to-date(assemblyCompilationNotRequired)를 구분한다. Unity가 재컴파일 불필요로 확인한 어셈블리를 새로 컴파일했다고 표현하지 않는다. 실행 ID·콜백 카운터는 SessionState로 도메인 재로딩을 넘어 복원하며, 콜백을 놓친 채 실행기가 비활성화되거나 Editor 재시작으로 세션이 사라지면 interrupted로 처리한다.

## M2b 편집 계약 (2026-09-07)

brain_read_edit(taskId, expectedRevision, nodeId)로 기존 Code/Document 하나의 content·summary·expectedHash·expectedContextHash·writable을 읽는다. Code 해시는 파일 bytes SHA256이고 Document 해시는 UnityBrainJson으로 직렬화한 전체 노드 payload SHA256이다. 서로 다른 해시 의미를 혼용하지 않는다. 문서 context 해시는 본문·연결 코드·의미 관계 기준이며 코드/관계 변경도 갱신 충돌로 거절한다.

brain_apply는 nodeId에 연결된 기존 프로젝트 .cs 하나만 허용한다. 작업 허용 경로·revision·예상 해시가 맞아야 쓰며 경로는 GUID에서 해석한다. 생성/삭제/이동/meta/바이너리는 미지원. UTF-8 128KiB 이하, NUL 불허. 읽기 결과의 BOM 문자와 줄바꿈은 호출자가 유지한다. 변경 후 needsAssetRefresh=true이면 Ivan assets-refresh를 실행한다. 자동 재컴파일을 시작해 응답을 잃지 않도록 apply 자체는 refresh하지 않는다.

brain_update_document는 nodes Document의 summary/body만 갱신한다. 연결 코드 경로가 모두 허용 범위여야 하고 expectedHash와 expectedContextHash를 함께 검사한다. ID/type/title/관계·작업 기준선은 변경하지 않는다. 본문이 바뀌면 status=unreviewed, updatedUtc를 갱신한다. 사람 review나 freshness는 자동 변경하지 않는다. 작성한 내용과 코드 근거를 확인한 뒤 brain_record_basis를 명시적으로 호출할 수 있으며 이는 사람 승인이 아니다. 기존 docs 사본과 자동 동기화하지 않는다.

편집 이력은 .projectbrain/edits/<UUID>.json의 actor=agent, 작업/revision/노드/작업 종류, 전후 해시/시각/state다. 쓰기 전 prepared를 저장하고 파일과 최종 이력 저장까지 성공한 뒤 applied를 반환한다. 중간 실패는 prepared를 남기고 경로가 포함된 오류를 반환한다. 일부 내용이 이미 바뀌었을 수 있으므로 재조회·해시 확인 뒤 재시도하며 원본 복원을 자동 강제하지 않는다. 동일 내용은 unchanged로 반환하고 이력을 만들지 않는다. 파일+이력의 다중 파일 트랜잭션이나 외부 편집기와의 OS 수준 CAS는 보장하지 않는다. prepared의 자동 복구/정리 및 이력 요약 UI는 후속이다.
