/*------------------------------------------------------------------
LightmapOptimizer : this tool optimizes lightmap by cutting it into
smaller blocks and saving them accordingly.
------------------------------------------------------------------*/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class LightmapToolsWindow : EditorWindow 
{
    private float colorThreshold = 0.05f;
	private bool chunkCut_bttn = false;
	private bool chunkPiece_bttn = false;
	private bool isCutting = false;
	private bool isPiecing = false;
	private string msg_01 = "";
	private string msg_02 = "";
	private string msg_03 = "";
	public Texture2D tex = null;
	public bool chunkCut_foldout = true;
	private int chunkResSelected = 256;
	private string[] chunkResString = new string[9] {"4", "8", "16", "32", "64", "128", 
		"256", "512", "1024"};
	private int[] chunkResValue = new int[9] {4, 8, 16, 32, 64, 128, 256, 512, 1024};
	private int textureRes = 0;
	private int chunkRes = 0;
	private int chunkNumberInRow = 1;
	private string texPath = "";
	private string parentFolder = "";
	private string texFileName = "";
	private string texAssetPath = "";


	[MenuItem("Tools/Lightmap 工具")]
	static void Init() 
	{
		//Get existing open window
		//If none, create a new one
		LightmapToolsWindow window = 
			(LightmapToolsWindow)EditorWindow.GetWindow (typeof(LightmapToolsWindow));
		window.title = "Lightmap 工具";
		window.Show ();
	}

	void OnGUI()
	{
		chunkCut_foldout = EditorGUI.Foldout (new Rect(3, 5, position.width - 20, 15), 
		                                      chunkCut_foldout, "Lightmap 优化");
		//Display content for chunkCutting subMenu if enable
		if (chunkCut_foldout) 
		{
			tex = EditorGUI.ObjectField (new Rect(14, 27, position.width - 20, 16), 
				"添加 Lightmap", tex, typeof(Texture2D), true) as Texture2D;
			chunkResSelected = EditorGUI.IntPopup(new Rect(14, 45, position.width - 20, 16), 
				"切块大小", chunkResSelected, chunkResString, chunkResValue);
			chunkCut_bttn = GUI.Button(new Rect(14, 65, position.width / 2 - 15, 16), 
				"切片优化");
			chunkPiece_bttn = GUI.Button(new Rect(position.width / 2 + 5, 65, 
				position.width / 2 - 11, 16), "拼合导入");
			EditorGUI.LabelField (new Rect(14, 85, position.width - 20, 16), msg_01);
			EditorGUI.LabelField (new Rect(14, 85, position.width - 20, 16), msg_02);
			EditorGUI.LabelField (new Rect(14, 85, position.width - 20, 16), msg_03);

			if (tex)
			{
				msg_01 = "";
				EditorGUI.DrawPreviewTexture(new Rect(14, 108, Mathf.Min(position.width - 20, 
					position.height - 115), Mathf.Min(position.width - 20, 
				    	position.height - 115)), tex);

				textureRes = tex.height;
				chunkRes = chunkResSelected;
				chunkNumberInRow = textureRes/chunkRes;
				texPath = AssetDatabase.GetAssetPath(tex);
				parentFolder = Path.GetDirectoryName(texPath);
				texFileName = Path.GetFileNameWithoutExtension(texPath);

				if (chunkCut_bttn)
					isCutting = true;
				if (chunkPiece_bttn)
					isPiecing = true;
			}
			else
				if (chunkCut_bttn || chunkPiece_bttn)
					msg_01 = "请先添加 Lightmap";
		}
	}

	void Update()
	{
		if (isCutting == true) 
		{
			chunkCut_function();
			Repaint();
			isCutting = false;
		}
		if (isPiecing == true)
		{
			chunkPiece_function();
			Repaint();
			isPiecing = false;
		}
	}

	/*
	------------------------------------------------------------------
	chunkCut_function : This function cuts the original lightmap into
	small blocks.
	------------------------------------------------------------------
	*/
	void chunkCut_function()
	{
		int row = 0;
		int col = 0;
		int ordChk = 0;
		int avgChk = 0;
		int blkChk = 0;
		msg_02 = "";
		msg_03 = "";
		int totalChunkPix = chunkRes * chunkRes;
		int totalChk = chunkNumberInRow * chunkNumberInRow;
		TexFileOperator (texPath, "r");//Set texture readable
		Color[] pix = tex.GetPixels ();//Color from 0 to 1 in float

		//Create main asset for lightmap chunk asset
		Color blackColor = new Color (0f, 0f, 0f, 1f);
		Texture2D texAsset = new Texture2D (1, 1, TextureFormat.RGBAFloat, false);
		texAsset.SetPixels (new Color[1]{blackColor});
		texAsset.Apply ();
		texAsset.Compress (false);
		texAssetPath = parentFolder + "/" + texFileName + "_" + 
			chunkResSelected.ToString() + ".asset";
		AssetDatabase.CreateAsset (texAsset, texAssetPath);

		Debug.Log ("切块优化进行中......0/" + totalChk.ToString ());
		int logID = 1;
		while (row < chunkNumberInRow) 
		{
			col = 0;
			while (col < chunkNumberInRow) 
			{
				//For every chunk
				Debug.Log ("切块优化进行中......" + logID.ToString () + 
				           "/" + totalChk.ToString ());
				Color[] optiChunk_1D = new Color[chunkRes * chunkRes];
				//Calculate the average color of current chunk
				float r = 0f;
				float g = 0f;
				float b = 0f;
				for (int px = 0; px < chunkRes; px++) 
				{
					for (int py = 0; py < chunkRes; py++) 
					{
						int currentID = 
							(row * chunkRes + px) * textureRes + col * chunkRes + py;
						r += pix [currentID].r;
						g += pix [currentID].g;
						b += pix [currentID].b;
					}
				}
				Color averageColor = new Color ((r / totalChunkPix), 
					(g / totalChunkPix), (b / totalChunkPix), 1f);

				//For every pixel
				//Focus on current chunk
				int averageChunkPix = 0;
				int blackChunkPix = 0;
				for (int px = 0; px < chunkRes; px++) 
				{
					for (int py = 0; py < chunkRes; py++) 
					{
						int currentID = (row * chunkRes + px) * textureRes + 
							col * chunkRes + py;
						Color currentColor = new Color (pix [currentID].r, 
							pix [currentID].g, pix [currentID].b, 1f);
						//If current color equals to black 
						//then blackChunkPix++ and drop it
						//If current color falls into average color's range 
						//then averageChunkPix++
						if ((currentColor.r == blackColor.r) &&
						    (currentColor.g == blackColor.g) &&
						    (currentColor.g == blackColor.b))
							blackChunkPix++;
						else 
						{
							if ((currentColor.r >= (averageColor.r - colorThreshold)) &&
							    (currentColor.r <= (averageColor.r + colorThreshold)))
								if ((currentColor.g >= (averageColor.g - colorThreshold)) &&
								    (currentColor.g <= (averageColor.g + colorThreshold)))
									if ((currentColor.b >= (averageColor.b - colorThreshold)) &&
									    (currentColor.b <= (averageColor.b + colorThreshold)))
										averageChunkPix++;
							optiChunk_1D [(px * chunkRes + py)] = currentColor;
						}
					}
				}
				//Finish scanning every pixel in current chunk
				if (blackChunkPix != totalChunkPix) 
				{
					if (averageChunkPix == totalChunkPix) 
					{
						//Save current chunk as single pixel
						Texture2D chkTex = new Texture2D (1, 1, TextureFormat.RGBAFloat, false);
						chkTex.name = "R" + row.ToString () + "/C" + col.ToString ();
						chkTex.wrapMode = TextureWrapMode.Clamp;
						chkTex.SetPixels (new Color[1]{averageColor});
						chkTex.Apply ();
						chkTex.Compress (false);
						AssetDatabase.AddObjectToAsset (chkTex, texAsset);
						AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath (chkTex));
						avgChk++;
					} 
					else 
					{
						//Save chunk texture to disk
						Texture2D chkTex = new Texture2D (chunkRes, chunkRes, 
						                                  TextureFormat.RGBAFloat, false);
						chkTex.name = "R" + row.ToString () + "/C" + col.ToString ();
						chkTex.wrapMode = TextureWrapMode.Clamp;
						chkTex.SetPixels (optiChunk_1D);
						chkTex.Apply ();
						chkTex.Compress (false);
						AssetDatabase.AddObjectToAsset (chkTex, texAsset);
						AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath (chkTex));
						ordChk++;
					}
				}
				col++;
				logID++;
			}
			row++;
		}
		//Finish scanning all chunks
		TexFileOperator (texPath, "ur");//Set texture back to unreadable
		blkChk = totalChk - avgChk - ordChk;
		string logString = "_O" + ordChk.ToString () + "A" + avgChk.ToString () + "B" + 
			blkChk.ToString ();
		string nameWithLog = texFileName + "_" + chunkResSelected.ToString () + logString;

		//Delete old files if any
		bool cleanOldFile = false;
		string[] oldFileGUID = AssetDatabase.FindAssets (nameWithLog, 
			(new string[1]{parentFolder}));
		if (oldFileGUID.Length > 0) 
		{
			string oldFilePath = AssetDatabase.GUIDToAssetPath (oldFileGUID [0]);
			cleanOldFile = AssetDatabase.DeleteAsset(oldFilePath);
			if (cleanOldFile == false)
				Debug.LogError ("无法删除之前的切片文件");
		}
		else
			cleanOldFile = true;
		//Rename asset with log information
		if (cleanOldFile == true) 
			AssetDatabase.RenameAsset (texAssetPath, nameWithLog);
		string logWithResult = "切块优化完成-->普通切块:" + ordChk.ToString () + 
			" / 单色切块:" + avgChk.ToString () + " / 黑色切块:" + blkChk.ToString ();
		Debug.Log (logWithResult);
		msg_02 = logWithResult;
	}

	/*
	------------------------------------------------------------------
	chunkPiece_function : This function restores the original lightmap
	from small blocks.
	------------------------------------------------------------------
	*/
	void chunkPiece_function()
	{
		msg_02 = "";
		string nameToFind = texFileName + "_" + chunkResSelected.ToString () + 
			" t:texture2D";
		string[] fileGUID = AssetDatabase.FindAssets (nameToFind,
		                                              (new string[1]{parentFolder}));
		
		//If chunk file exists
		if (fileGUID.Length > 0) 
		{
			texAssetPath = AssetDatabase.GUIDToAssetPath (fileGUID [0]);
			object[] chkArray = AssetDatabase.LoadAllAssetsAtPath(texAssetPath);
			if (chkArray.Length > 1)
			{
				//If chunk asset file contains valid information
				Color[] piecedTex = new Color[textureRes * textureRes];
				for (int chkID = 0; chkID < chkArray.Length; chkID++)
				{
					//For every chunk
					if (chkArray[chkID].ToString()[0] == 'R')
					{
						int[] pos = ExtractPosFromName(chkArray[chkID].ToString());
						int row = pos[0];
						int col = pos[1];
						Color[] chkTex = (chkArray[chkID] as Texture2D).GetPixels ();
						
						//Average chunk with single pixel
						if ((chkArray[chkID] as Texture2D).width == 1)
						{
							for (int px = 0; px < chunkRes; px++) 
							{
								for (int py = 0; py < chunkRes; py++) 
								{
									int texPixID = (row * chunkRes + px) * textureRes + 
										col * chunkRes + py;
									piecedTex[texPixID] = chkTex[0];
								}
							}
						}
						//Do ordinary chunk
						if ((chkArray[chkID] as Texture2D).width > 1)
						{
							for (int px = 0; px < chunkRes; px++) 
							{
								for (int py = 0; py < chunkRes; py++) 
								{
									int chkPixID = px * chunkRes + py;
									int texPixID = (row * chunkRes + px) * textureRes + 
										col * chunkRes + py;
									piecedTex[texPixID] = chkTex[chkPixID];
								}
							}
						}
					}
				}
				//Re-create whole lightmap texture
				Texture2D reLM = new Texture2D (textureRes, textureRes, 
				                             TextureFormat.RGBAFloat, false);
				reLM.name = "OptiLM";
				reLM.wrapMode = TextureWrapMode.Clamp;
				reLM.SetPixels (piecedTex);
				reLM.Apply ();
				reLM.Compress (false);

				LightmapData[] sceneLMArray = LightmapSettings.lightmaps;
				LightmapData updatedLM = new LightmapData();
				for (int lmID = 0; lmID < sceneLMArray.Length; lmID++)
				{
					updatedLM.lightmapFar = reLM;
					sceneLMArray[lmID] = updatedLM;
				}
				LightmapSettings.lightmaps = sceneLMArray;
				Debug.Log ("SceneLMArray: " + sceneLMArray.Length.ToString());
			}
			if (chkArray.Length == 1)
			{
				//No chunk texture in asset file except one pixel black
				Debug.LogError("切片文件中没有有效信息，请重新切片");
				msg_03 = "切片文件中没有有效信息，请重新切片";
			}
		}
		else 
		{
			//Cannot find any chunk asset file
			Debug.LogError("没有找到切片文件，请先进行切片优化");
			msg_03 = "没有找到切片文件，请先进行切片优化";
		}
	}

	/*
	------------------------------------------------------------------
	TexFileOperator : This function sets texture file readable or 
	unreadable.
	------------------------------------------------------------------
	*/
	void TexFileOperator(string tPath, string cmd)
	{
		var texImporter = AssetImporter.GetAtPath(tPath) as TextureImporter;
		if (cmd == "r") //Set texture readable
		{
			if (texImporter != null) 
			{
				texImporter.isReadable = true;
				AssetDatabase.ImportAsset (tPath);
			}
		}
		if (cmd == "ur") //Set texture unreadable
			if (texImporter != null)
				texImporter.isReadable = false;
	}

	/*
	------------------------------------------------------------------
	ExtractPosFromName : This function extracts position information
	from the name.
	------------------------------------------------------------------
	*/
	int[] ExtractPosFromName(string n)
	{
		string[] posSplit = n.Split('/', ' ');
		string r = posSplit[0].Substring(1);
		string c = posSplit[1].Substring(1);
		return (new int[2]{int.Parse(r), int.Parse(c)});
	}
}
