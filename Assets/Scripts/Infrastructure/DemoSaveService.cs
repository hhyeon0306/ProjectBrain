using UnityEngine;

namespace ProjectBrain.Demo.Infrastructure
{
    /// <summary>데모 세션 결과를 로컬 저장소에 기록한다.</summary>
    public sealed class DemoSaveService : MonoBehaviour
    {
        public void Save(int health, Vector3 position)
        {
            PlayerPrefs.SetInt("Demo.Health", health);
            PlayerPrefs.SetString("Demo.Position", JsonUtility.ToJson(position));
            PlayerPrefs.Save();
        }
    }
}
