﻿using System.Composition;
using Prime.Core;

namespace Prime.IPFS.Win64
{
    [Export(typeof(IExtensionExecute))]
    public class IpfsWin64Extension : IpfsExtension
    {
        public override Platform Platform => Platform.WinAmd64;
    }
}