using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "Naknak/Tutorial Case", fileName = "TutorialCase")]
public sealed class TutorialCaseDefinition : ScriptableObject
{
    [Serializable]
    public sealed class Objective
    {
        public SceneReference scene = new SceneReference();
        public List<DialogCondition> conditions = new List<DialogCondition>();
        [TextArea] public string text;
    }
    public TMP_FontAsset font;
    public ConditionID solved;
    public List<Objective> objectives = new List<Objective>();
}
