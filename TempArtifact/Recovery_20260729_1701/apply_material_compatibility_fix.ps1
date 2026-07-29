$ErrorActionPreference = 'Stop'

$backupDirectory = 'TempArtifact\Recovery_20260729_1701'
$shaderRelativePath = 'Assets\Shaders\MobileGpuSkinnedUnit.shader'
$rendererRelativePath = 'Assets\Scripts\DefenseGame\GpuSkinnedUnitBatchRenderer.cs'
$utf8WithoutBom = [Text.UTF8Encoding]::new($false)

Copy-Item -LiteralPath $shaderRelativePath -Destination (Join-Path $backupDirectory 'MobileGpuSkinnedUnit.before_alias_fix.shader')
Copy-Item -LiteralPath $rendererRelativePath -Destination (Join-Path $backupDirectory 'GpuSkinnedUnitBatchRenderer.before_alias_fix.cs')

function Replace-Once {
    param(
        [string] $Text,
        [string] $Old,
        [string] $New,
        [string] $Label
    )

    $index = $Text.IndexOf($Old, [StringComparison]::Ordinal)
    if ($index -lt 0 -or $Text.IndexOf($Old, $index + $Old.Length, [StringComparison]::Ordinal) -ge 0) {
        throw "Expected exactly one match: $Label"
    }

    return $Text.Substring(0, $index) + $New + $Text.Substring($index + $Old.Length)
}

$shaderPath = (Resolve-Path -LiteralPath $shaderRelativePath).Path
$shaderText = [IO.File]::ReadAllText($shaderPath)
$shaderText = Replace-Once $shaderText `
    '        _MainTex("Legacy Base Map", 2D) = "white" {}' `
    "        _MainTex(`"Legacy Base Map`", 2D) = `"white`" {}`r`n        [HideInInspector] _UseLegacyMainTex(`"Use Legacy Main Texture`", Float) = 0" `
    'legacy texture property'
$shaderText = Replace-Once $shaderText `
    '                half4 _EmissionColor;' `
    "                half4 _EmissionColor;`r`n                half _UseLegacyMainTex;" `
    'legacy texture cbuffer'

$oldSurfaceSample = @'
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uvBase);
                half4 legacySample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvLegacy);
                half4 tint = (half4)_GpuSkinColors[input.instanceID];
                half4 albedoAlpha = baseSample * legacySample * _BaseColor * _Color * tint;
'@ -replace "`n", "`r`n"
$newSurfaceSample = @'
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uvBase);
                half4 legacySample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvLegacy);
                half4 surfaceSample = lerp(baseSample, legacySample, saturate(_UseLegacyMainTex));
                half4 tint = (half4)_GpuSkinColors[input.instanceID];
                half4 albedoAlpha = surfaceSample * _BaseColor * _Color * tint;
'@ -replace "`n", "`r`n"
$shaderText = Replace-Once $shaderText $oldSurfaceSample $newSurfaceSample 'surface sample selection'
[IO.File]::WriteAllText($shaderPath, $shaderText, $utf8WithoutBom)

$rendererPath = (Resolve-Path -LiteralPath $rendererRelativePath).Path
$rendererText = [IO.File]::ReadAllText($rendererPath)
$methodAnchor = @'
				}
			}

			private static void EnsureBuffer
'@ -replace "`n", "`r`n"
$materialCompatibilityBlock = @'
				}

				bool useLegacyMainTexture = !source.HasProperty("_BaseMap") && source.HasProperty("_MainTex");
				destination.SetFloat("_UseLegacyMainTex", useLegacyMainTexture ? 1f : 0f);
				if (source.HasProperty("_BaseColor"))
				{
					destination.SetColor("_Color", Color.white);
				}
				else
				{
					destination.SetColor("_BaseColor", Color.white);
				}
				bool alphaClip = source.IsKeywordEnabled("_ALPHATEST_ON") || (source.HasProperty("_AlphaClip") && source.GetFloat("_AlphaClip") > 0.5f);
				destination.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
				if (alphaClip)
				{
					destination.EnableKeyword("_ALPHATEST_ON");
				}
				else
				{
					destination.DisableKeyword("_ALPHATEST_ON");
				}
			}

			private static void EnsureBuffer
'@ -replace "`n", "`r`n"
$rendererText = Replace-Once $rendererText $methodAnchor $materialCompatibilityBlock 'material compatibility block'
[IO.File]::WriteAllText($rendererPath, $rendererText, $utf8WithoutBom)
