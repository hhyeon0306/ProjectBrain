# 2026-09-06 문서 전면 검토

## 결론

최종 목업의 방향과 Unity-MCP 재사용 결정은 유지할 근거가 있다. 다만 이전 문서는 제품 목표, 현재 저장소, 향후 완료 규칙을 섞어 적었고 단계 의존성과 제출 준비 기준이 부족했다. **A1은 현재 작업 사본의 저장·이관 기본 범위 완료이며, 제품 전체나 안전한 계층 그래프 완료가 아니다.** 새 Git 체크아웃의 줄바꿈 때문에 이관이 거절되는 문제를 실제 재현했다.

문서의 명확한 모순·과도한 표현은 이번에 수정했다. 구현 결함과 미결정 설계는 해결됐다고 쓰지 않고 작업표에 남겼다. 제품 C#·씬·실제 .projectbrain 데이터는 변경하지 않았다. A2는 시작하지 않았다. 특정 모델이 작성했다는 이유로 평가하지 않고 코드·실행·출처를 기준으로 검토했다.

## 검토 범위와 기준점

- 상위 AGENTS + Docs Markdown 5개, Brain AGENTS + Docs Markdown 7개, archive Markdown 7개: 기존 작성 문서 총 21개.
- 양쪽 session-start/docs-check/verify 스크립트 6개와 관련 Git 설정·이력.
- BrainNode/BrainStore/BrainMigration, 자체 검사 함수, 기존 문서 서비스/UI/관계 그래프, 실제 JSON·receipt, asmdef, 설치 MCP 패키지의 테스트 실행 전제.
- 사용자 제공 최종 목업의 구조. 현재 Unity 창의 시각 검토는 수행하지 않았다.
- 상위 기준 커밋 2dccc3c60eed3d37a3a711bbbedfd99c78fc652e, Brain 기준 7fd9d660a0eeef4a27183df61e1f33c08cca8ae8.
- archive는 과거 판단의 보존본으로 읽었으며 폐기된 외부 제품 조사 링크를 최신 구현 근거로 재사용하지 않았다. 사용자 생성 문서 외의 패키지 매뉴얼·자동 생성 스킬 전체는 검토 범위가 아니다.

## 주요 발견과 조치

| ID | 우선순위 | 발견 | 이번 조치 / 남은 일 |
|---|---|---|---|
| R1 | 높음 | bytes 기반 이관 해시와 Git 줄바꿈 정책 충돌. 내용 변경 없이 재실행 거절 | 임시 Git checkout + Unity 이관 복제본에서 재현. 코드·설정 미수정, A1-R 등록 |
| R2 | 높음 | 저장소의 관계 참조 검사를 계층·증거 의미 검증으로 오인할 여지 | 실제 순환 contains와 Domain을 verified_by 대상으로 저장 가능. 현재 보장 축소 명시, A2/W2 규칙 미구현으로 기록 |
| R3 | 높음 | A2 완료 조건이 A3/V1의 실제 증거를 선행 요구. 필수 P1 context 시연은 권장 M2에 의존 | A2를 계층·자료 탐색으로 한정하고 증거는 미검증 표시. M2a context 필수 / M2b 편집 흐름 분리 |
| R4 | 높음 | 새 UI의 쓰기 기준과 기존 docs 편집이 분리돼 있는데 통합 방침이 없음 | A2 착수 계약에 저장 기준·기존 창 경계 명시. Markdown 안의 role/design/cautions를 구조화 필드로 오인하지 않게 수정 |
| R5 | 높음 | 사람 확인·문서 해시·검증 범위·상태 전이·완료 성공의 구체 계약 부족 | 목표에 문서/코드 변경 무효화·확인 주체·0개 테스트 등을 복원. 필드/정책의 구현은 W1/W2/A3/V1에 남김 |
| R6 | 중간 | 현재 저장 트리에 미래 tasks/evidence/activities가 섞임. “UnityBrainJson만 Unity 의존” 범위 과장 | 실제 트리와 예정 기록 분리. 레거시 DocumentStore/BrainMigration 의존과 단일 Editor asmdef 명시 |
| R7 | 중간 | “57개 검사 통과”를 NUnit·플레이·UI 또는 제품 Evidence로 읽을 수 있음 | 자체 함수의 체크 항목 25/18/14임을 명시. 과거 결과를 이번 실행 결과로 재사용하지 않음 |
| R8 | 중간 | AGENTS가 참조하는 사용 지침 전환 조건이 현재 명세에서 빠짐. archive README는 자신을 최신 기준으로 안내 | 완전한 도구 흐름 확인 조건 복원. archive 입구에 비권위 보존본 표시 및 현재 문서 링크 추가 |
| R9 | 중간 | updatedUtc가 자동 저장 시각처럼 설명되고 F2 실제 UI 재확인 미완료가 작업표에 드러나지 않음 | 호출자 제공 시각으로 정정. F2 구현 완료와 UI 재확인 대기 분리 |
| R10 | 중간 | 지원서/PPT 확보·제출 완료의 구체 기준이 없고 Brain과 같은 A1/A2 ID 사용 | 상위/하위 ID 구분, 자료·문항·첨부·접수 증거 확인 항목 보완. 실제 자료는 미확보로 유지 |
| R11 | 낮음 | 관리 verify가 모든 문서·제품 데이터를 검증하는 것처럼 읽힐 여지, 과거 연결 안내가 현재형으로 남음 | 검사 범위와 제외·사용자 기존 변경에 의한 거절을 명시. MCP 연결 성공은 시점별 관찰로 구분 |

## R1: Git 체크아웃과 이관의 재현성

근거: [BrainMigration.cs](../../Packages/com.projectbrain.editor/Editor/BrainMigration.cs)의 HashFile/SameSources 및 완료 receipt 검사(기준 커밋 55, 159, 175행 부근). core.autocrlf=true, docs는 index LF / working LF이며 해당 경로의 .gitattributes 정책은 없었다.

원본: docs/10000000000000000000000000000003.json.
- 현재 SHA256: a9cfd5c691d809c94ec1256d06a5abd94691b5228cd1eb28c0df66411519dab2
- 동일 index에서 autocrlf=true로 임시 체크아웃한 SHA256: d9a7d48d3ea46e0c40eef85eb7de36184908a339cfd7cc73b3a91a8c315dc522

재현 절차:
1. OS temp의 새 폴더를 git checkout-index --prefix 대상으로 지정하고 위 문서 하나만 체크아웃한다. 실제 작업 사본은 변경하지 않는다.
2. 실제 .projectbrain JSON을 다른 임시 폴더에 복사하고 해당 원본 문서만 체크아웃 결과로 바꾼다.
3. 복제본에서 BrainMigration.Run을 호출한다.
4. “이관 후 원본 문서가 변경됐습니다” InvalidDataException 발생을 확인했다.

문제는 데이터 삭제가 아니라 재현·재실행 거절이다. A1-R에서는 추적 데이터의 줄바꿈 고정 또는 정규화된 비교 계약 중 하나를 정하고 기존 receipt와의 호환을 검증해야 한다. 원본 보존 요구를 이유 없이 약화하거나 receipt를 삭제하는 방식으로 처리하지 않는다.

## R2 / R9: 실제 저장소 보장 확인

[BrainStore.cs](../../Packages/com.projectbrain.editor/Editor/BrainStore.cs)의 SaveNode, ValidateRelations, ValidateNode와 대조했다. 격리된 임시 저장소에서 다음을 실행했다.

- Domain alpha와 beta 사이에 contains를 양방향 저장: 성공. 계층 순환을 거절하지 않는다.
- Domain alpha → Domain beta의 verified_by 저장: 성공. Evidence type을 검사하지 않는다.
- title만 바꿔 다시 SaveNode: updatedUtc는 기존 값 유지. 자동 저장 시각이 아니다.

이는 기존 57항목 검사에 포함되지 않은 경계다. 관계별 의미 규칙을 어느 계층에 둘지는 후속 구현 결정이며, 이번에는 저장소 코드를 바꾸지 않았다. contains 규칙이 아직 없다고 모든 depends_on 순환까지 금지하도록 요구하지 않는다.

실제 프로젝트를 읽은 결과는 노드 15개·관계 26개다. 임시 시험 결과와 실제 데모 데이터를 혼합하지 않았다.

## R4 / R5: A2 이후 재작업을 줄이기 위한 결정

[BrainDocumentWindow.cs](../../Packages/com.projectbrain.editor/Editor/BrainDocumentWindow.cs)는 ScriptDocumentService를 통해 docs를 편집한다. 새 BrainNode에는 role/designIntent/cautions 전용 필드가 없고 이관은 텍스트 body로 보존한다. 새 오른쪽 패널이 독립 필드를 편집하려면 데이터 형식 결정이 먼저다.

또한 A1에서 Evidence/Activity는 일반 노드일 뿐 실행 상세·코드 스냅샷·문서 확인 기록은 없다. 노드와 상세 기록의 원본 책임, 사람 확인 경로, 감시 범위와 허용 범위, 검증 중 변경 처리 및 정책은 명세에서 “계획/미결정”으로 구분했다. 문서를 보완한 것과 해당 기능의 구현 완료는 별개다.

## 검증 주장과 현재 관리 검사

25+18+14는 Unity script-execute로 호출한 RunInEditor 자체 함수 안의 성공 체크 수다. 기존 구현 실행 기록은 work_log와 A1 커밋에 있고 NUnit XML·공식 Test Runner 57케이스·게임 플레이 증거로 볼 수 없다. 이번 리뷰에서 전체 자체 검사를 불필요하게 반복하지 않았다.

관리 docs-check는 정해진 필수 문서의 존재·일부 링크·기록 필드와 Git 변경에 따른 기록 갱신을 검사한다. 모든 Markdown 의미, 모든 링크, 제품 JSON, 미구현 기능, 완료 기준 충족을 검사하지 않는다. 의미 있는 변경 필터에 .projectbrain은 들어 있지 않다. 기존 사용자/Unity 변경만 남아도 기록 변경을 요구할 수 있어, 커밋 전 통과를 커밋 후 항상 통과하는 보장으로 쓰지 않는다. 검사 스크립트 개선은 이번 문서 검토에서 수행하지 않았다.

## 공식 일정·제출 정보 재확인

2026-09-06 [공식 공고와 FAQ](https://www.nexon-tutorial.com/) 본문을 확인했다. 마감 9/7 16:00, AI면접 9/12 오후, AI활용 역량평가 10/3 오후, 최종 제출 후 수정 불가 안내는 현재 기록과 일치한다. 포트폴리오는 필수가 아닌 권장 자료다. Brain은 지원자 선택 산출물이며 공식 필수 제출 요건으로 표현하면 안 된다.

실제 지원서 문항·글자 수·첨부 형식/용량·사용자 제출 상태는 공개 공고 확인으로 알 수 없고 아직 미확인이다. 최종 제출은 사용자 수행 원칙을 유지하며, 완료는 접수 화면/마이페이지/확인 메일 등 실제 증거로 확인한다.

## 보존된 판단과 미검증

- 최종 목업·도메인 중심 구조·Unity-MCP 재사용·기존 영상 유지·서류/제출 시간 확보는 유지한다.
- archive의 과거 선택과 work_log의 과거 결과는 사실 기록으로 보존하고 현재 기준으로 덮어쓰지 않았다. 다음 세션이 archive를 현재 지침으로 읽지 않도록 안내만 정정했다.
- 이번 재현은 제한된 사례이며 모든 데이터 손상·동시성·플랫폼 호환 문제를 망라하지 않는다.
- 사용자 자료·실제 지원 내용·최종 제출, 전체 UI 품질, 플레이 동작, 전체 코드 품질/보안 리뷰는 수행하지 않았다.
- 제품 코드와 사용자/Unity 기존 변경 두 파일은 수정하지 않았다. 연결 설정·사용량·리셋권도 변경하지 않았다.

## 실행 결과 원문 요약

다음 값은 이번 리뷰의 실제 MCP 결과에서 발췌했다. true인 accepted는 기능 성공이 아니라 현재 거절하지 않는 경계를 뜻한다.

```json
{
  "storageProbe": [
    {
      "name": "fixture",
      "value": "C:\\Users\\Public\\Documents\\ESTsoft\\CreatorTemp\\BrainDocAudit-9b109521875940f2b4770413364a7e45"
    },
    {
      "name": "containsCycleAccepted",
      "value": true
    },
    {
      "name": "domainAsEvidenceTargetAccepted",
      "value": true
    },
    {
      "name": "savedTimestampUnchanged",
      "value": true
    },
    {
      "name": "realNodes",
      "value": 15
    },
    {
      "name": "realRelations",
      "value": 26
    },
    {
      "name": "compiling",
      "value": false
    }
  ],
  "checkoutMigrationProbe": [
    {
      "name": "fixture",
      "value": "C:\\Users\\Public\\Documents\\ESTsoft\\CreatorTemp\\BrainCheckoutMigrationAudit-274a87fdd62d48dcb373e09440c5d1a2"
    },
    {
      "name": "checkoutOnlyChangeRejected",
      "value": true
    },
    {
      "name": "reason",
      "value": "이관 후 원본 문서가 변경됐습니다. 자동 덮어쓰기 대신 명시적 재조정이 필요합니다."
    }
  ]
}
```

## 문서 수정 검증

수정 후 Markdown 22개(기존 21개 + 이 보고서)의 로컬 링크 52개와 코드 펜스 균형을 확인했다. 깨진 로컬 경로와 불균형 펜스 없음. scripts/verify.ps1 -IncludeBrain 커밋 전 통과. archive 7개는 안내 2줄씩만 추가했고 과거 본문을 유지했다. 제품 코드·실제 JSON diff 없음. 이후 남는 사용자/Unity 기존 변경 두 파일은 이 문서 커밋에서 제외한다.
