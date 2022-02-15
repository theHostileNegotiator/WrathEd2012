using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using SAGE.WrathEdXML.GameDefinition;

namespace SAGE
{
	public class StreamDefinition
	{
		public string id { get; private set; }
		public string Description { get; private set; }
		public bool IsNameRequired { get; private set; }

		public StreamDefinition(StreamType stream)
		{
			id = stream.Name;
			Description = stream.Description;
			IsNameRequired = stream.IsNameRequired;
		}
	}

	public class GameDefinition
	{
		public string id { get; private set; }
		public string inheritDefinitions { get; private set; }
		public short ManifestVersion { get; private set; }
		public uint AllTypesHash { get; private set; }
		public string WorldBuilderVersion { get; private set; }
		public ColorType ThemeColor { get; private set; }
		public string DocPath { get; private set; }
		public List<StreamDefinition> Streams { get; private set; }
		public AssetDefinition Assets { get; private set; }
		public string DefinitionPath { get; private set; }
		public string BaseDefinitionPath { get; private set; }

		public GameDefinition(Game game, Func<string, bool> status)
		{
			if (game != null)
			{
				id = game.id;
				inheritDefinitions = game.inheritDefinitions;
				ManifestVersion = game.ManifestVersion;
				AllTypesHash = game.AllTypesHash;
				WorldBuilderVersion = game.WorldBuilderVersion;
				ThemeColor = game.ThemeColor;
				if (Registry.LocalMachine.OpenSubKey(game.RegistryEntry.Key) != null
					&& Registry.LocalMachine.OpenSubKey(game.RegistryEntry.Key).GetValue(game.RegistryEntry.Value) != null)
				{
					DocPath = string.Format(
						"{0}{1}{2}{1}Mods",
						Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
						Path.DirectorySeparatorChar,
						Registry.LocalMachine.OpenSubKey(game.RegistryEntry.Key).GetValue(game.RegistryEntry.Value).ToString());
				}
				else
				{
					DocPath = string.Empty;
				}
				Streams = new List<StreamDefinition>();
				foreach (StreamType stream in game.Stream)
				{
					Streams.Add(new StreamDefinition(stream));
				}
				DefinitionPath = string.Format(
					"{0}{1}Games{1}{2}",
					Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
					Path.DirectorySeparatorChar,
					id);
				Assets = new AssetDefinition();
				getAssetDefinitions(new DirectoryInfo(DefinitionPath), status);
				if (!String.IsNullOrEmpty(inheritDefinitions))
				{
					BaseDefinitionPath = string.Format(
						"{0}{1}Games{1}{2}",
						Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
						Path.DirectorySeparatorChar,
						inheritDefinitions);

					getInheritedAssetDefinitions(new DirectoryInfo(BaseDefinitionPath), status, id, inheritDefinitions);
				}
				foreach (BaseAssetType asset in Assets.AssetTypes)
				{
					if (asset.GetType() == typeof(AssetType))
					{
						foreach (BaseAssetType otherAsset in Assets.AssetTypes)
						{
							if (otherAsset.GetType() == typeof(AssetType))
							{
								AssetType otherAssetType = otherAsset as AssetType;
								if (otherAssetType.Entries != null
									&& otherAssetType.Entries[0].GetType() == typeof(EntryInheritanceType)
									&& (otherAssetType.Entries[0] as EntryInheritanceType).AssetType == asset.id)
								{
									asset.Subclasses.Add(otherAssetType);
									otherAssetType.Superclass = asset;
								}
							}
						}
					}
				}
				foreach (GameAssetType asset in Assets.GameAssetTypes)
				{
					foreach (GameAssetType otherAsset in Assets.GameAssetTypes)
					{
						if (otherAsset.Entries != null
							&& otherAsset.Entries[0].GetType() == typeof(EntryInheritanceType)
							&& (otherAsset.Entries[0] as EntryInheritanceType).AssetType == asset.id)
						{
							asset.Subclasses.Add(otherAsset);
							otherAsset.Superclass = asset;
						}
					}
				}
			}
			else
			{
				id = "None";
			}
		}

		private void getAssetDefinitions(DirectoryInfo source, Func<string, bool> status)
		{
			foreach (DirectoryInfo directoryInfo in source.GetDirectories())
			{
				getAssetDefinitions(directoryInfo, status);
			}
			foreach (FileInfo fileInfo in source.GetFiles())
			{
				if (fileInfo.Extension != ".xml")
				{
					continue;
				}
				status(fileInfo.Name);
				Assets.Merge(new AssetDefinition(WrathEdXML.AssetDefinition.AssetDefinition.Load(fileInfo.FullName)));
			}
		}
		
		private void getInheritedAssetDefinitions(DirectoryInfo source, Func<string, bool> status, string CurrentGameDefinition, string BaseGameDefinition)
		{
			foreach (DirectoryInfo directoryInfo in source.GetDirectories())
			{
				getInheritedAssetDefinitions(directoryInfo, status, CurrentGameDefinition, BaseGameDefinition);
			}
			foreach (FileInfo basefileInfo in source.GetFiles())
			{
				if (basefileInfo.Extension != ".xml")
				{
					continue;
				}

				string ToolPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

				string BaseGameDefPath = source.FullName.Substring(ToolPath.Length);
				string GameDefPath = ToolPath + BaseGameDefPath.Replace(BaseGameDefinition, CurrentGameDefinition);
				
				DirectoryInfo GameDefPathDir = new DirectoryInfo(GameDefPath);
				
				bool DefFileExist = false;
				
				if (GameDefPathDir.Exists){
					foreach (FileInfo fileInfo in GameDefPathDir.GetFiles())
					{
						if (fileInfo.Extension != ".xml")
						{
							continue;
						}
						if (basefileInfo.Name == fileInfo.Name)
						{
							DefFileExist = true;
							break;
						}
					}
				}
				if (!DefFileExist)
				{
					status(basefileInfo.Name);
					Assets.Merge(new AssetDefinition(WrathEdXML.AssetDefinition.AssetDefinition.Load(basefileInfo.FullName)));
				}
				
			}
		}
	}
}
