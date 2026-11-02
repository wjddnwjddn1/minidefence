using UnityEditor;
using UnityEngine;
namespace EmberBastion.Editor {
 public sealed class PixelArtImport : AssetPostprocessor {
  void OnPreprocessTexture(){if(!assetPath.StartsWith("Assets/Resources/")||!assetPath.EndsWith(".png"))return;var t=(TextureImporter)assetImporter;t.npotScale=TextureImporterNPOTScale.None;t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.textureCompression=TextureImporterCompression.Uncompressed;t.alphaIsTransparency=true;if(assetPath.Contains("/FrostCast")||assetPath.EndsWith("/FxRepair.png"))t.maxTextureSize=4096;}
 }
}
