﻿using Buran.Core.Library.Utils;
using Buran.Core.MvcLibrary.Data.Attributes;
using Buran.Core.MvcLibrary.Reflection;
using Buran.Core.MvcLibrary.Utils;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Resources;
using System.Text;

namespace Buran.Core.MvcLibrary.Extenders
{
    [HtmlTargetElement("select2dropdown")]
    public class Select2DropDownTagHelper : TagHelper
    {
        [HtmlAttributeName("brn-field")]
        public ModelExpression ModelItem { get; set; }

        [HtmlAttributeName("brn-items")]
        public SelectList ItemList { get; set; }

        [HtmlAttributeName("brn-items2")]
        public List<SelectListItem> ItemList2 { get; set; }

        [HtmlAttributeName("brn-placeholder")]
        public string PlaceHolderText { get; set; }

        [HtmlAttributeName("brn-multi-select")]
        public bool MultiSelect { get; set; }

        [HtmlAttributeName("brn-cssclass")]
        public string CssClass { get; set; }

        [HtmlAttributeName("brn-can-clear-select")]
        public bool CanClearSelect { get; set; }

        [HtmlAttributeName("brn-disable-editor-template")]
        public bool DisableEditorTemplate { get; set; }

        [HtmlAttributeName("brn-disable-form-group")]
        public bool DisableFormGroup { get; set; }

        [HtmlAttributeName("brn-disable-js")]
        public bool DisableJs { get; set; }


        [HtmlAttributeName("brn-add-new-url")]
        public string AddNewUrl { get; set; }

        [HtmlAttributeName("brn-label-col")]
        public int LabelColCount { get; set; }
        [HtmlAttributeName("brn-editor-col")]
        public int EditorColCount { get; set; }

        [HtmlAttributeName("brn-width")]
        public int Width { get; set; }



        [HtmlAttributeName("brn-isrequired")]
        public bool? IsRequired { get; set; }

        [HtmlAttributeName("brn-required-css-class")]
        public string RequiredCssClass { get; set; }

        [HtmlAttributeName("brn-symbol")]
        public string Symbol { get; set; }




        private IHtmlHelper _htmlHelper;
        private IServiceProvider _serviceProvider;

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public Select2DropDownTagHelper(IHtmlHelper htmlHelper, IServiceProvider provider)
        {
            LabelColCount = 3;
            EditorColCount = 9;
            _htmlHelper = htmlHelper;
            _serviceProvider = provider;

            RequiredCssClass = "editor-field-required";
            Symbol = " *";
        }

        private ComboBoxDataInfo GetComboBoxDataSource(ModelExplorer metadata)
        {
            ComboBoxDataInfo result = new ComboBoxDataInfo();
            var comboDataModel = Digger2.GetMetaAttr<ComboBoxDataAttribute>(metadata.Metadata);
            if (comboDataModel != null)
            {
                result.CanSelect = comboDataModel.ShowSelect;

                if (comboDataModel.Repository != null)
                {
                    var repo = comboDataModel.Repository;
                    if (repo != null)
                    {
                        var obj = ActivatorUtilities.CreateInstance(_serviceProvider, repo);
                        var a = repo.GetMethod(comboDataModel.QueryName);
                        if (a == null)
                            return null;
                        if (a.GetParameters().Length == 1)
                        {
                            var dataList = a.Invoke(obj, new object[1] { metadata.Model });
                            result.ListItems = dataList as SelectList;
                        }
                        else if (a.GetParameters().Length == 2)
                        {
                            var dataList = a.Invoke(obj, new object[2] { metadata.Model, false });
                            result.ListItems = dataList as SelectList;
                        }
                    }
                }
            }
            return result;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            (_htmlHelper as IViewContextAware).Contextualize(ViewContext);
            var prefix = ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix;

            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            if (Width > 0)
                output.Attributes.Add("style", $"width:{Width}px");

            var htmlId = prefix.IsEmpty() ? ModelItem.Metadata.PropertyName : _htmlHelper.IdForModel() + "_" + ModelItem.Metadata.PropertyName;
            var htmlName = prefix.IsEmpty() ? ModelItem.Metadata.PropertyName : prefix + "." + ModelItem.Metadata.PropertyName;

            var labelText = ModelItem.Metadata.DisplayName ?? ModelItem.Metadata.PropertyName ?? htmlId.Split('.').Last();

            if (ItemList == null)
            {
                var comboDataInfo = GetComboBoxDataSource(ModelItem.ModelExplorer);
                ItemList = comboDataInfo.ListItems;
            }
            var sbOptions = new StringBuilder();
            if (CanClearSelect)
                sbOptions.AppendLine(string.Concat("<option value=\"\"></option>"));
            if (ItemList != null)
            {
                foreach (var item in ItemList)
                {
                    var option = new TagBuilder("option");
                    option.Attributes.Add("value", item.Value);
                    if (item.Selected)
                        option.Attributes.Add("selected", "selected");
                    option.InnerHtml.AppendHtml(item.Text);
                    sbOptions.AppendLine(option.GetString());
                }
            }
            else if (ItemList2 != null)
            {
                foreach (var item in ItemList2)
                {
                    var option = new TagBuilder("option");
                    option.Attributes.Add("value", item.Value);
                    if (item.Selected)
                        option.Attributes.Add("selected", "selected");
                    option.InnerHtml.AppendHtml(item.Text);
                    sbOptions.AppendLine(option.GetString());
                }
            }

            var select = new TagBuilder("select");
            select.AddCssClass("form-control input-sm");
            if (!CssClass.IsEmpty())
                select.AddCssClass(CssClass);
            select.Attributes.Add("name", htmlName);
            select.Attributes.Add("id", htmlId);
            if (MultiSelect)
                select.Attributes.Add("multiple", "multiple");

            if (!IsRequired.HasValue && ModelItem.Metadata.GetIsRequired())
            {
                var requiredAttr = Digger2.GetMetaAttr<RequiredAttribute>(ModelItem.Metadata);
                var errMsg = "";
                if (requiredAttr != null)
                {
                    errMsg = requiredAttr.ErrorMessage;
                    if (errMsg.IsEmpty() && requiredAttr.ErrorMessageResourceType != null)
                    {
                        var rm = new ResourceManager(requiredAttr.ErrorMessageResourceType);
                        var rsm = rm.GetString(requiredAttr.ErrorMessageResourceName);
                        if (rsm != null && !rsm.IsEmpty())
                            errMsg = string.Format(rsm, labelText);
                    }
                }
                select.Attributes.Add("data-val", "true");
                select.Attributes.Add("data-val-required",
                    requiredAttr != null
                        ? errMsg
                        : "Gereklidir"
                );
            }
            select.InnerHtml.AppendHtml(sbOptions.ToString());
            string js = DisableJs
                ? ""
                : $@"
<script type=""text/javascript"">
$(function () {{
    $(""#{htmlId}"").select2({{
        placeholder: ""{(PlaceHolderText.IsEmpty() ? "" : PlaceHolderText)}"",
        {(CanClearSelect ? "allowClear: true" : "")}
    }});
}});
</script>";

            if (DisableEditorTemplate)
            {
                if (!AddNewUrl.IsEmpty())
                {
                    var sep = "?";
                    if (AddNewUrl.Contains("?"))
                        sep = "&";
                    AddNewUrl += sep + "editorId=" + htmlId;
                    output.Content.SetHtmlContent($@"
<div class=""input-group"">
    {select.GetString()}
    <span class=""input-group-btn"">
        <a href='{AddNewUrl}' class='btn btn-xs btn-primary btnAddPopup fancyboxAdd fancybox.iframe'><i class='fa fa-plus'></i></a>
    </span>
    {js}
</div>");
                }
                else
                {
                    output.Content.SetHtmlContent($@"{select.GetString()}
{js}");
                }
            }
            else
            {
                if (!DisableFormGroup)
                {
                    output.Attributes.Add("class", "form-group");
                    output.Attributes.Add("id", "div" + htmlId);
                }
                else
                {
                    output.TagName = "span";
                }

                var irq = (!IsRequired.HasValue && ModelItem.Metadata.GetIsRequired()) || (IsRequired.HasValue && IsRequired.Value);
                var requiredHtml = irq
                    ? $"<span class=\"{RequiredCssClass}\">{Symbol}</span>"
                    : "";

                var metaHtml = irq
                    ? $"<span class=\"field-validation-valid help-block\" data-valmsg-for=\"{htmlId}\" data-valmsg-replace=\"true\"></span>"
                    : "";

                if (!AddNewUrl.IsEmpty())
                {
                    var sep = "?";
                    if (AddNewUrl.Contains("?"))
                        sep = "&";
                    AddNewUrl += sep + "editorId=" + htmlId;
                    output.Content.SetHtmlContent($@"
<label class=""col-xs-{LabelColCount} control-label"">{labelText} {requiredHtml}</label>
<div class=""col-xs-{EditorColCount}"">
    <div class=""input-group"">
        {select.GetString()}
        <span class=""input-group-btn"">
            <a href='{AddNewUrl}' class='btn btn-xs btn-primary btnAddPopup fancyboxAdd fancybox.iframe'><i class='fa fa-plus'></i></a>
        </span>
    </div>
    {metaHtml}
</div>
{js}");
                }
                else
                {
                    output.Content.SetHtmlContent($@"
<label class=""col-xs-{LabelColCount} control-label"">{labelText} {requiredHtml}</label>
<div class=""col-xs-{EditorColCount}"">
    {select.GetString()}
    {metaHtml}
</div>
{js}");
                }
            }
        }
    }
}
