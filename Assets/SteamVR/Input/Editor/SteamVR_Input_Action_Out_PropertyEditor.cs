using UnityEditor;
using UnityEngine;

using System.CodeDom;
using Microsoft.CSharp;
using System.IO;
using System.CodeDom.Compiler;

using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;


[CustomPropertyDrawer(typeof(SteamVR_Input_Action_Out))]
public class SteamVR_Input_Action_Out_PropertyEditor : SteamVR_Input_Action_GenericPropertyEditor<SteamVR_Input_Action_Out>
{
}