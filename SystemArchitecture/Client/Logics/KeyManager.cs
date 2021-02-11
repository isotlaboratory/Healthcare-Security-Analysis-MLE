using System;
using System.IO;
using Microsoft.Research.SEAL;

namespace CDTS_PROJECT.Logics
{

    public interface IKeyManager
    {
        SecretKey LoadSecretKey();
        PublicKey LoadPublicKey();
        KeyPair CreateKeys();
    }

    public class KeyPair{

        public PublicKey publicKey { get;}
        public SecretKey secretKey { get;}

        public KeyPair( PublicKey _publicKey, SecretKey _secretKey){
            publicKey = _publicKey;
            secretKey = _secretKey;
        }

        
    }

    public class KeyManager : IKeyManager

    {
        private readonly ContextManager _contextManager;
        public KeyManager(ContextManager contextManager)
        {
            _contextManager = contextManager;
        }


        public PublicKey LoadPublicKey()
        {
            
            PublicKey pk = new PublicKey();
            MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes("pk.txt"));
            pk.Load(_contextManager.Context, memoryStream);
            
            return pk;


        }

        public SecretKey LoadSecretKey()
        {
            SecretKey sk = new SecretKey();
            MemoryStream memoryStream2 = new MemoryStream(File.ReadAllBytes("sk.txt"));
            sk.Load(_contextManager.Context, memoryStream2);
            return sk;

        }
            

        public KeyPair CreateKeys()
        {
            KeyGenerator keyGenerator = new KeyGenerator(_contextManager.Context);
            PublicKey pk = keyGenerator.PublicKey;
            SecretKey sk = keyGenerator.SecretKey;

            return new KeyPair(pk, sk);

        }
    }
}
