# 2026-09-06 Project Brain 기반 코드 검토

## 판정

**기본 구조는 유지할 수 있지만 현재 구현을 결함 없는 출발점으로 승인할 수는 없다.** 기존 정상 경로는 실행되며 저장·이관 기본 검사는 다시 통과했다. 추가 실패 조건에서 확정 결함 3건을 확인했다. 프로젝트를 새로 만드는 대신 저장 안전성·Git 재현성·UI 오류 경계를 먼저 보완하는 것이 적절하다.

사용자는 A2 전 프로젝트 자체의 시작점을 검토해 달라고 요청했다. 이번에는 검토·재현·기록만 수행했다. 제품 C#·원본/이관 데이터·사용자 씬·설정은 수정하지 않았고 A2도 시작하지 않았다. 코드 수정은 다음 작업이다.

## 확정 결함

### P1 — 필수 스키마 누락을 기본값으로 보충해 손상 보호를 우회한다

근거: [BrainNode.cs](../../Packages/com.projectbrain.editor/Editor/BrainNode.cs)의 schemaVersion/relations 초기값, [UnityBrainJson.cs](../../Packages/com.projectbrain.editor/Editor/UnityBrainJson.cs)의 FromJson, [BrainStore.cs](../../Packages/com.projectbrain.editor/Editor/BrainStore.cs)의 LoadRelations/SaveRelations, [DocumentStore.cs](../../Packages/com.projectbrain.editor/Editor/DocumentStore.cs)의 읽기·버전 검증.

Unity 6000.3.8f1에서 실제 확인:
- `JsonUtility.FromJson<BrainRelationFile>("{}")`는 schemaVersion=1과 null이 아닌 빈 relations를 만든다.
- `relations.json={}`를 LoadRelations로 읽으면 손상 오류 대신 0개를 반환한다. 이어 SaveRelations를 호출하면 이 불완전 파일이 정상 파일처럼 덮어써진다.
- 정상 노드 JSON에서 schemaVersion 필드를 제거해도 LoadNode가 성공한다.
- scriptGuid만 있는 레거시 문서도 명시적 버전 없이 v2로 받아들인다.

유효한 명시적 빈 관계 파일과 필수 필드가 빠진 파일을 구분하지 못한다. 현재 실제 데이터가 유실됐다는 증거는 없지만, 파일 훼손·불완전 출력 때 이를 정상 상태로 확정할 위험이 있다. 기존 “not json”과 schemaVersion=99 검사만으로는 잡히지 않았다.

보완: 생성 기본값과 읽기 검증을 분리해 필수 필드의 존재·형식을 확인한다. 누락 버전, 누락 관계 배열, 명시적 빈 배열, v1 호환, 손상 기존 파일 덮어쓰기 거절을 회귀 검사로 추가한다. 임의 JSON 정규식만으로 전체 구조를 판별하지 않는다. **A2 전 저장소 보완 대상**이다.

### P1 — 정상 Git 복제본의 이관 재실행이 실패한다

근거: [BrainMigration.cs](../../Packages/com.projectbrain.editor/Editor/BrainMigration.cs)의 SameSources/HashFile과 현재 Git core.autocrlf=true, 줄바꿈 정책 부재. [문서 검토 보고서](2026-09-06-document-review.md)의 부분 체크아웃 재현에 더해 이번에는 실제 깨끗한 로컬 clone을 만들었다.

- 기준 커밋: 1f9bd8f7159c30fb379fab608b8bfe58e1d0ec1e.
- clone 직후 Git 변경 0개.
- 이 복제본의 .projectbrain에 현재 로드된 BrainMigration.Run을 실행하면 원본 변경 InvalidDataException으로 거절된다.
- 원인은 JSON 내용이 같은데 LF→CRLF로 bytes 해시가 달라지기 때문이다.

보완: 추적 문서의 줄바꿈 정책과 원본 해시 계약을 정하고 기존 receipt와의 호환을 확인한다. 실제 새 clone에서 원본 보존·재실행 성공을 확인해야 한다. receipt 삭제나 실제 변경을 무시하는 방식으로 우회하지 않는다. clone에서 새 Unity Editor를 실행·컴파일한 것은 아니므로 이 결과를 클린 환경 전체 빌드 검사라고 표현하지 않는다.

### P2 — 관계 조회 오류가 UI 처리 경계를 벗어나 문서 구성을 중단한다

근거: [BrainDocumentWindow.cs](../../Packages/com.projectbrain.editor/Editor/BrainDocumentWindow.cs) 45/61행의 보호되지 않은 BuildForm 호출과 65–75행의 화면 초기화·전체 문서 관계 조회.

별도 임시 DocumentStore와 숨겨진 새 EditorWindow 인스턴스로 재현했다. 선택 문서는 정상인데 다른 문서 하나가 손상된 경우 CreateGUI → BuildForm → GetGraphRelatedGuids에서 InvalidDataException이 Run 처리 밖으로 전파된다. form과 graph를 비운 뒤의 실패이므로 사용자에게 설명된 오류 상태를 구성하지 못한다.

전체 관계 조회를 실패시키는 저장 정책 자체를 조용히 무시하라는 뜻은 아니다. 보완: UI 진입점의 오류를 일관되게 처리하고 손상 문서 위치·다음 행동을 보여주며 현재 문서와 미저장 상태를 보존한다. 정상 문서만 몰래 제외해 성공처럼 표시하지 않는다. 기존 문서 UI 유지 또는 A2 서비스/화면 이관 시 함께 처리해야 한다.

## 확인한 정상 기반

| 영역 | 이번 확인 |
|---|---|
| 연결 | Unity MCP 실제 씬 조회·코드 실행 성공, ProjectBrain/Assets 대상 확인 |
| 실행 상태 | Unity 6000.3.8f1, isCompiling=false. 확인 시 최근 10분 Error 로그 0건 |
| 패키지 | Brain 0.1.0은 임베디드 UPM, Editor 전용 asmdef. 런타임용 제품 코드를 별도 서버에 중복 구현하지 않음 |
| 기존 검사 | BrainStoreChecks 25, BrainMigrationChecks 18, DocumentStoreChecks 14항목 이번 실행에서 통과 |
| 실제 데이터 | 15노드/26관계 읽기 성공, 연결 자산 8개의 GUID 해석 누락 없음 |
| 자산 식별 | Assets와 Brain 패키지 meta 93개 모두 GUID 해석, 중복 GUID 그룹 0. 검사한 C#/asmdef의 meta 누락 없음 |
| 기본 UI 기능 | 임시 문서와 숨겨진 창에서 실제 그래프 버튼 콜백으로 이동·역방향 복귀 성공 |
| Git 구분 | 상위/하위 독립 저장소, 실제 코드·데이터에 이번 변경 없음 |

UI 콜백 검사는 클릭 좌표·레이아웃·화면 크기·사용자 미저장 취소 대화상자의 시각 QA가 아니다. 자체 검사 수는 NUnit/Test Runner 수가 아니며 추가 경계 재현 실패를 포함해 “전체 통과”라고 표현하지 않는다.

## 구조 판단과 아직 없는 기능

유지할 판단:
- 최종 도메인 계층/상세 패널 목표와 Editor 전용 UPM 패키지 방향.
- 저장소 규칙과 UI/MCP를 논리적으로 분리하고 기존 Unity-MCP를 재사용하는 방향.
- 원본 v2 문서를 보존하고 한 도메인의 시연부터 완성하는 순서.

향후 단계로 남길 사항:
- 관계 type별 대상 검사, contains 순환·부모 규칙은 A2에 필요하다. 현재 일반 관계 저장소가 이를 보장하지 않는다.
- 역할·설계 의도·주의사항은 이관 Document.body 텍스트다. 독립 편집 UI 전에 필드 계약을 정한다.
- Evidence/Activity 종류의 노드 저장은 가능하지만 상세 실행 기록·사람 확인·작업 완료 규칙은 없다.
- 전체 서비스가 별도 Core 어셈블리로 분리된 것은 아니다. 레거시 DocumentStore는 Unity JsonUtility에 의존한다.
- 데모 7개 스크립트는 구조 설명 자료다. GameFlow의 참조 연결, 입력 공급, HUD 갱신 등 실제 플레이 씬이 준비됐다고 볼 수 없다. 현재 작업은 게임 출시용 Player 빌드 검증이 아니다.
- 동시 작성·외부 변경 감지·삭제 복구·변조 방지는 현재 저장소 보장 밖이다. 추후 W1/W2 계약으로 다룬다.

이러한 미구현을 모두 기존 A1의 버그로 묶거나 즉시 대규모 재설계할 이유는 없다. 다만 위 확정 결함은 기존 파일 보호·운영 흐름에 관한 것이므로 새 UI 확장 전에 처리한다.

## 검토 범위와 한계

- Brain 패키지 C# 12개, 데모 C# 7개, asmdef/package manifest/lock, 관련 설정·meta/Git·실데이터를 검토했다.
- 주요 실패 조건은 임시 디렉터리·숨겨진 독립 창에서 재현했다. 실제 사용자 창을 닫거나 씬을 저장·재생하지 않았다.
- 기존 사용자/Unity 변경인 ProjectSettings의 MCP define과 Unsafe.dll.meta의 Editor 활성 설정을 읽기만 했다. 설치 플러그인은 의존성 구성과 define 보완 경로를 갖고 있으며 이 변경만으로 오류라고 단정하지 않는다. 이 둘은 이번 커밋에서 제외한다.
- 현재 로드된 코드의 검사를 실행했다. 새 clone의 Unity 최초 import·패키지 복원·MCP 연결, 전체 강제 재컴파일, Player 빌드, 화면 시각 QA, 실제 게임 플레이, 장시간/동시성/전체 보안 검사는 수행하지 않았다.
- 오픈소스 패키지 전체를 재감사하거나 Library 코드를 수정하지 않았다. 패키지 버전 업그레이드는 이번 검토 범위가 아니다.
- 임시 clone과 재현 fixture는 OS temp에 보존했다. raw 결과와 재현 C#은 [검토 실행 증거](2026-09-06-project-review-evidence.json)에 있다. 이는 개발 검토 자료이며 제품 Evidence 저장 기능은 아니다.

## 다음 작업의 완료 기준

1. A1-R: 필수 JSON 필드 누락 거절과 줄바꿈/해시 재현성 보완.
2. F2: 손상 문서 상태를 안내하는 UI 오류 경계 보완.
3. 기존 자체 검사 + 새 실패 회귀 사례 + 깨끗한 clone 데이터 재실행을 검증한다.
4. 결과를 기록하고 그때 A2를 시작할 기반으로 다시 판단한다.

이 검토 시점에는 위 수정이 **미실행**이다. 최종 UI나 범용 도구를 새로 늘리는 대신 확인된 소수 결함부터 해결하면 된다.

## 기록 검증

커밋 전 scripts/verify.ps1 -IncludeBrain 통과. Markdown 23개·로컬 링크 66개·코드 펜스·실행 증거 JSON 구문 확인 통과. 제품 코드·원본/이관 JSON·사용자 씬/설정에 이번 변경 없음. 기존 사용자/Unity 변경 두 파일은 보존한다.
