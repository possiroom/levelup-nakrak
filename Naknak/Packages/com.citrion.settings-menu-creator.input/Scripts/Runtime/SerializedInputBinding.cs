using System;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator.Input
{
  [Serializable]
  public class SerializedInputBinding
  {
    [SerializeField]
    [HideInInspector]
    protected string binding;

    public SerializedInputBinding() { }

    public SerializedInputBinding(string binding)
    {
      this.binding = binding;
    }

    public string Binding { get => binding; set => binding = value; }
  } 
}
