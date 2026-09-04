using UnityEngine;

namespace ProjectBrain.Demo
{
    /// <summary>Brain 문서의 연결·저장·변경 감지를 확인하기 위한 샘플. 자동 테스트가 아닙니다.</summary>
    public sealed class BrainDocumentSample : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 3f;

        // 자동으로 움직이지 않습니다. 호출한 경우에만 월드 방향으로 이동합니다.
        public void Move(Vector3 direction, float deltaTime)
        {
            if (deltaTime <= 0f) return;
            transform.position += Vector3.ClampMagnitude(direction, 1f) * moveSpeed * deltaTime;
        }
    }
}
