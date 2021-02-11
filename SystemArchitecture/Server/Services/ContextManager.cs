using System;
using Microsoft.Research.SEAL;

namespace CDTS_PROJECT.Services
{

	public interface IContextManager{
		EncryptionParameters EncryptionParams { get; set; }
		SEALContext Context { get; set; }
	}

	public class ContextManager : IContextManager
	{
		public ContextManager()
		{
			EncryptionParams = new EncryptionParameters(SchemeType.BFV);
			const ulong polyModulusDegree = 2048;
			EncryptionParams.PolyModulusDegree = polyModulusDegree;
			EncryptionParams.CoeffModulus = CoeffModulus.BFVDefault(polyModulusDegree);
			EncryptionParams.PlainModulus = new Modulus(1024);
			Context = new SEALContext(EncryptionParams);
		}

		public EncryptionParameters EncryptionParams { get; set; }
		public SEALContext Context { get; set; }
	}
}
