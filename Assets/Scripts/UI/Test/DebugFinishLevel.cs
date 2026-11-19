using UnityEngine;
using UnityEngine.UI;

namespace SaKabilangPinto.UI
{
    /// <summary>Test only: a button that finishes the level with sample counts.</summary>
    public class DebugFinishLevel : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] CompleteBox completeBox;
        [SerializeField] int flames = 3;
        [SerializeField] int fragments = 2;

        void Awake()
        {
            button.onClick.AddListener(() => completeBox.Show(flames, fragments));
        }
    }
}
