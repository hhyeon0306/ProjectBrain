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
            transform.position += new Vector3(move.x, 0f, move.y) * speed * Time.deltaTime;
        }
    }
}
