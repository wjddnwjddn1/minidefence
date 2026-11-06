using System;
using EmberBastion.PixelFX;
using UnityEditor;
using UnityEngine;
namespace EmberBastion.Editor {
 public static class PixelFxCheck {
  public static void Run(){AssetDatabase.Refresh();string path="Assets/PixelFX/Generated/impacts/hit-spark/hit-spark.json";Selection.activeObject=AssetDatabase.LoadAssetAtPath<TextAsset>(path);if(Selection.activeObject==null)throw new Exception("PixelFX metadata missing");PixelFxImport.ImportSelected();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixelFX/Generated/impacts/hit-spark/hit-spark.prefab");if(prefab==null)throw new Exception("PixelFX prefab missing");var player=prefab.GetComponent<PixelFxPlayer>();var sheet=SheetReader.Read(player.metadata,player.texture);if(sheet.frames.Length!=16||sheet.meta.pixelfx.id!="impacts/hit-spark")throw new Exception("PixelFX frames invalid");var go=UnityEngine.Object.Instantiate(prefab);try{go.SendMessage("Awake");go.GetComponent<PixelFxPlayer>().Play();if(go.GetComponent<SpriteRenderer>().sprite==null)throw new Exception("PixelFX first frame missing");for(int i=0;i<4;i++)go.SendMessage("Update");Debug.Log("PIXELFX_UNITY_PREFAB_OK frames="+sheet.frames.Length);}finally{UnityEngine.Object.DestroyImmediate(go);}}
 }
}
