﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using TeaCommerce.Api.Models;
using TeaCommerce.Api.Services;
using umbraco.cms.businesslogic.packager.standardPackageActions;
using umbraco.interfaces;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;
using ContentExtensions = umbraco.ContentExtensions;
using File = System.IO.File;

namespace TeaCommerce.StarterKit.Install {
  public class TeaCommerceStarterKitInstall : IPackageAction {
    public string Alias() {
      return "TeaCommerceStarterKitInstaller";
    }

    public bool Execute( string packageName, XmlNode xmlData ) {
      IMediaService mediaService = ApplicationContext.Current.Services.MediaService;
      IContentService contentService = UmbracoContext.Current.Application.Services.ContentService;
      IContentTypeService contentTypeService = ApplicationContext.Current.Services.ContentTypeService;

      //Get store
      IReadOnlyList<Store> stores = StoreService.Instance.GetAll().ToList();
      Store store = stores.FirstOrDefault();
      if ( store == null ) {
        store = new Store( "Starter Kit Store" );
        store.Save();
      }

      //Update languages and products
      IReadOnlyList<IContent> langContents = contentService.GetByLevel( 1 ).Where( c => c.ContentType.Alias == "Lang" && !c.Published && c.CreateDate > DateTime.Now.AddMinutes( -5 ) ).ToList();
      if ( langContents.Any() ) {
        foreach ( IContent langContent in langContents ) {
          IContent productSearchPage = langContent.Descendants().Where( c => c.ContentType.Alias == "ProductSearchPage" ).FirstOrDefault();
          langContent.SetValue( "featuredProducts", string.Join( ",", products.Take( 4 ).Select( c => c.Id ) ) );
          langContent.SetValue( "store", store.Id );
          contentService.Save( langContent );

          int count = 0;
          foreach ( IContent productContent in products ) {
            contentService.Save( productContent );
            count++;
          }

          //Fix Cart step templates
          IReadOnlyList<IContent> cartSteps = langContent.Descendants().Where( c => c.ContentType.Alias == "CartStep" ).OrderBy( c => c.SortOrder ).ToList();
          if ( cartSteps.Any() ) {

            IContentType contentType = contentTypeService.GetContentType( cartSteps.First().ContentTypeId );
            IEnumerable<ITemplate> templates = contentType.AllowedTemplates;
            count = 2;
            foreach ( IContent cartStep in cartSteps ) {
              string templateAlias = "CartStep" + count;
              ITemplate template = templates.FirstOrDefault( t => t.Alias == templateAlias );
              if ( template != null ) {
                cartStep.Template = template;
                contentService.Save( cartStep );
              }
              count++;
            }
          }
        }
      }



      return true;

    }

    public XmlNode SampleXml() {
      return helper.parseStringToXmlNode( string.Format( @"<Action runat=""install"" alias=""{0}"" />", Alias() ) );
    }

    public bool Undo( string packageName, XmlNode xmlData ) {
      //Remove stuff
      return true;
    }
  }
}
