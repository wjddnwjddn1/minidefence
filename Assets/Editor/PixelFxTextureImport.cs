using UnityEditor;
using UnityEngine;
namespace EmberBastion.Editor {
 public sealed class PixelFxTextureImport : AssetPostprocessor {
  void OnPreprocessTexture(){if(!assetPath.StartsWith("Assets/PixelFX/Generated/")&&!assetPath.StartsWith("Assets/Resources/PixelFX/"))return;var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.maxTextureSize=8192;}
 }
}
