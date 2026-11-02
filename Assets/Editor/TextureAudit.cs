using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace EmberBastion.Editor {
 public static class TextureAudit {
  public static void Run(){
   var lines=new StringBuilder();
   foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources"})){
    string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TextureImporter;var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(importer==null||texture==null)continue;int width,height;importer.GetSourceTextureWidthAndHeight(out width,out height);if(width!=texture.width||height!=texture.height)lines.AppendLine(path+" source="+width+"x"+height+" imported="+texture.width+"x"+texture.height+" limit="+importer.maxTextureSize);
   }
   File.WriteAllText("Docs/texture-audit.txt",lines.Length==0?"No texture dimensions changed.\n":lines.ToString(),new UTF8Encoding(false));
   if(lines.Length>0)throw new InvalidOperationException("Imported texture dimensions differ; see Docs/texture-audit.txt");
  }
 }
}
