using System;
using System.IO;
using EmberBastion.PixelFX;
using UnityEditor;
using UnityEngine;
namespace EmberBastion.Editor {
 public static class PixelFxImport {
  [MenuItem("Ember Bastion/PixelFX/Import Selected JSON")]
  public static void ImportSelected(){
   string jsonPath=AssetDatabase.GetAssetPath(Selection.activeObject);if(!jsonPath.EndsWith(".json",StringComparison.OrdinalIgnoreCase)){EditorUtility.DisplayDialog("PixelFX","Select a generated JSON asset first.","OK");return;}
   string pngPath=Path.ChangeExtension(jsonPath,".png").Replace('\\','/');var importer=AssetImporter.GetAtPath(pngPath) as TextureImporter;if(importer==null)throw new InvalidOperationException("PNG not found: "+pngPath);
   importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.SaveAndReimport();
   var json=AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);SheetReader.Read(json,texture);
   string prefab=Path.ChangeExtension(jsonPath,".prefab").Replace('\\','/');if(AssetDatabase.LoadAssetAtPath<GameObject>(prefab)!=null){EditorUtility.DisplayDialog("PixelFX","Prefab already exists; it was preserved.","OK");return;}
   var go=new GameObject(Path.GetFileNameWithoutExtension(jsonPath));try{go.AddComponent<SpriteRenderer>();var player=go.AddComponent<PixelFxPlayer>();player.metadata=json;player.texture=texture;PrefabUtility.SaveAsPrefabAsset(go,prefab);}finally{UnityEngine.Object.DestroyImmediate(go);}AssetDatabase.SaveAssets();Debug.Log("PixelFX prefab created: "+prefab);
  }
 }
}
