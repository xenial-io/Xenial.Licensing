﻿using System;
using System.ComponentModel;

using DevExpress.Persistent.Base;
using DevExpress.Xpo;

namespace Xenial.Licensing.Model
{
    [Persistent("Company")]
    [DefaultClassOptions]
    public class Company : XenialLicenseBaseObject
    {
        private string name;
        private string companyGlobalId;
        private Guid id = Guid.NewGuid();

        /// <summary>
        /// Initializes a new instance of the <see cref="Company"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public Company(Session session) : base(session) { }

        /// <summary>
        /// Invoked when the current object is about to be initialized after its creation.
        /// </summary>
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            CompanyGlobalId = Guid.NewGuid().ToString();
        }

        [Persistent("Id")]
        [Key(AutoGenerate = false)]
        public Guid Id { get => id; set => SetPropertyValue(ref id, value); }

        [Persistent("Name")]
        public string Name { get => name; set => SetPropertyValue(ref name, value); }

        [Persistent("CompanyGlobalId")]
        public string CompanyGlobalId { get => companyGlobalId; set => SetPropertyValue(ref companyGlobalId, value); }

        [Association("Company-User"), Aggregated]
        public XPCollection<CompanyUser> Users => GetCollection<CompanyUser>();
    }

    [Persistent("CompanyUser")]
    public class CompanyUser : XenialLicenseBaseObject
    {
        private string name;
        private string email;
        private Company company;
        private string id;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyUser"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public CompanyUser(Session session) : base(session) { }

        [Key(AutoGenerate = false)]
        [Persistent("Id")]
        [Size(100)]
        public string Id { get => id; set => SetPropertyValue(ref id, value); }

        [Persistent("Name")]
        public string Name { get => name; set => SetPropertyValue(ref name, value); }

        [Persistent("Email")]
        public string Email { get => email; set => SetPropertyValue(ref email, value); }

        [Persistent("CompanyId")]
        [Association("Company-User")]
        public Company Company { get => company; set => SetPropertyValue(ref company, value); }
    }
}
