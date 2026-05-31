// Copyright (c) GiantCroissant. All rights reserved.

using System.Runtime.CompilerServices;

namespace PlateShared.SCG.General.DisposePattern.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
