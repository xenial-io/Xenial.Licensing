﻿using System;
using System.Collections.Generic;

using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Xpo;

using Xenial.Licensing.Model;
using Xenial.Licensing.Module.BusinessObjects;
using Xenial.Licensing.Module.Infrastructure;
using Xenial.Framework.ModelBuilders;

namespace Xenial.Licensing.Module
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ModuleBase.
    public sealed partial class LicensingModule : ModuleBase
    {
        public LicensingModule()
            => InitializeComponent();

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB)
        {
            ModuleUpdater updater = new DatabaseUpdate.Updater(objectSpace, versionFromDB);
            return new ModuleUpdater[] { updater };
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo)
        {
            base.CustomizeTypesInfo(typesInfo);
            CalculatedPersistentAliasHelper.CustomizeTypesInfo(typesInfo);

            ModelBuilder.Create<Company>(typesInfo)
                .WithDetailViewLayout(l => new()
                {
                    l.TabbedGroup() with
                    {
                        Children = new()
                        {
                            l.Tab() with
                            {
                                Caption = "Information",
                                ImageName = "Action_AboutInfo",
                                Children = new()
                                {
                                    l.HorizontalGroup() with
                                    {
                                        Children = new()
                                        {
                                            l.PropertyEditor(m => m.Name),
                                            l.PropertyEditor(m => m.IsAutogenerated),
                                        }
                                    },
                                    l.HorizontalGroup() with
                                    {
                                        Children = new()
                                        {
                                            l.PropertyEditor(m => m.CompanyGlobalId),
                                            l.PropertyEditor(m => m.Id),
                                        }
                                    }
                                }
                            },
                            l.Tab() with
                            {
                                Caption = "Users",
                                ImageName = "Actions_User",
                                Children = new()
                                {
                                    l.PropertyEditor(m => m.Users)
                                }
                            }
                        }
                    }
                })
                .Build();
        }

        protected override IEnumerable<Type> GetDeclaredExportedTypes()
            => base.GetDeclaredExportedTypes()
                .UseLicensingPersistentModels()
                .UseLicensingViewModels();

        public override void Setup(XafApplication application)
        {
            base.Setup(application);
            application.SetupComplete -= Application_SetupComplete;
            application.SetupComplete += Application_SetupComplete;
        }

        private void Application_SetupComplete(object sender, EventArgs e)
        {
            Application.ObjectSpaceCreated -= Application_ObjectSpaceCreated;
            Application.ObjectSpaceCreated += Application_ObjectSpaceCreated;
        }

        private void Application_ObjectSpaceCreated(object sender, ObjectSpaceCreatedEventArgs e)
        {
            var objectSpace = e.ObjectSpace as CompositeObjectSpace;
            if (objectSpace != null)
            {
                if (!(objectSpace.Owner is CompositeObjectSpace))
                {
                    objectSpace.PopulateAdditionalObjectSpaces((XafApplication)sender);
                }
            }
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters)
        {
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new SingletonNavigationItemNodesUpdater());
            updaters.UseDetailViewLayoutBuilders();
        }
    }
}
