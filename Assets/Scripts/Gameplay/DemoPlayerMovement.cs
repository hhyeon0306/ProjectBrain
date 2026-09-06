using UnityEngine;

namespace ProjectBrain.Demo.Gameplay
{
    /// <summary>입력을 월드 이동으로 변환한다. 입력 공급은 외부에서 DemoPlayerInput.SetMove를 호출해 수행한다.</summary>
    public sealed class DemoPlayerMovement : MonoBehaviour
    {
        [SerializeField] private DemoPlayerInput input;
        [SerializeField, Min(0f)] private float speed = 4f;

        private void Update()
        {
            var move = input.Move;
            // 입력의 Y를 월드 Z로 옮긴다. Transform 직접 이동이며 충돌 처리는 포함하지 않는다.
            transform.position += new Vector3(move.x, 0f, move.y) * speed * Time.deltaTime;
        }
    }
}
