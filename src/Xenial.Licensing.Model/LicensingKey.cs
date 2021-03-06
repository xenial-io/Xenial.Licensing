﻿using DevExpress.Persistent.Base;
using DevExpress.Xpo;

using IdentityModel;

namespace Xenial.Licensing.Model
{
    [Persistent("LicensingKey")]
    [DefaultClassOptions]
    public class LicensingKey : XenialLicenseBaseObject
    {
        private string name;
        private string passPhrase;
        private string privateKey;
        private string publicKey;
        private string id = CryptoRandom.CreateUniqueId();

        /// <summary>
        /// Initializes a new instance of the <see cref="LicensingKey"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public LicensingKey(Session session) : base(session) { }

        [Persistent("Id")]
        [Key(AutoGenerate = false)]
        [Size(100)]
        [DevExpress.ExpressApp.Model.ModelDefault("AllowEdit", "False")]
        public string Id { get => id; set => SetPropertyValue(ref id, value); }

        [Persistent("Name")]
        public string Name { get => name; set => SetPropertyValue(ref name, value); }

        [Persistent("PassPhrase")]
        [DevExpress.ExpressApp.Model.ModelDefault("IsPassword", "True")]
        public string PassPhrase { get => passPhrase; set => SetPropertyValue(ref passPhrase, value); }

        [Persistent("PrivateKey")]
        [Size(SizeAttribute.Unlimited)]
        [DevExpress.ExpressApp.Model.ModelDefault("IsPassword", "True")]
        public string PrivateKey { get => privateKey; set => SetPropertyValue(ref privateKey, value); }

        [Persistent("PublicKey")]
        [Size(SizeAttribute.Unlimited)]
        public string PublicKey { get => publicKey; set => SetPropertyValue(ref publicKey, value); }
    }
}
