﻿namespace JsonLinq;

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;

internal static class RuntimeMetadataReference
{
	internal static readonly MetadataReference CorLibReference
		= MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

	private static ImmutableArray<string> _defaultAssemblyNames;
	private static ImmutableDictionary<string, string>? _trustedPlatformAssemblyMap;

	internal static ImmutableArray<string> DefaultAssemblyNames
	{
		get
		{
			if (_defaultAssemblyNames.IsDefault)
				ImmutableInterlocked.InterlockedInitialize(ref _defaultAssemblyNames, Create());

			return _defaultAssemblyNames;

			static ImmutableArray<string> Create()
			{
				return
				[
					"netstandard.dll",
					"System.Collections.dll",
					"System.Collections.Immutable.dll",
					"System.ComponentModel.TypeConverter.dll", 
					"System.Core.dll",
					"System.Globalization.dll",
					"System.IO.FileSystem.dll",
					"System.IO.FileSystem.Primitives.dll",
					"System.Linq.dll",
					"System.Linq.Expressions.dll",
					"System.ObjectModel.dll",
					"System.Runtime.dll",
					"System.Text.Encoding.dll",
					"System.Text.RegularExpressions.dll",
				];
			}
		}
	}

	internal static ImmutableDictionary<string, string> TrustedPlatformAssemblyMap
	{
		get
		{
			if (_trustedPlatformAssemblyMap is null)
				Interlocked.CompareExchange(ref _trustedPlatformAssemblyMap, CreateTrustedPlatformAssemblies(), null);

			return _trustedPlatformAssemblyMap;
		}
	}

	static ImmutableDictionary<string, string> CreateTrustedPlatformAssemblies()
	{
		return AppContext
			.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!
			.ToString()!
			.Split(';')
			.ToImmutableDictionary(f => Path.GetFileName(f));
	}
}
