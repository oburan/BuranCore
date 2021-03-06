﻿using Buran.Core.Library.Reflection;
using Buran.Core.Library.Utils;
using Buran.Core.MvcLibrary.AdminPanel.Controls;
using Buran.Core.MvcLibrary.AdminPanel.Utils;
using Buran.Core.MvcLibrary.LogUtil;
using Buran.Core.MvcLibrary.Reflection;
using Buran.Core.MvcLibrary.Resource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Buran.Core.MvcLibrary.AdminPanel.Controllers
{
    public class EditController<T, Z> : ListController<T, Z>
         where T : class, new()
         where Z : class
    {
        protected string ViewEditPopup = "EditPopup";
        protected string ViewEdit = "Edit";
        protected string ViewCreatePopup = "CreatePopup";
        protected string ViewCreate = "Create";

        protected string CreateAction = "Create";
        protected string CreateJsAction = "";
        protected string EditAction = "Edit";
        protected string EditJsAction = "";

        protected string CreateSaveAndCreateUrl = string.Empty;
        protected string CreateReturnListUrl = string.Empty;
        protected string EditReturnListUrl = string.Empty;

        protected EditController(bool popupEditor, Z context)
         : base(popupEditor, context)
        {
            if (popupEditor)
            {
                EditAction = "EditPopup";
                CreateAction = "CreatePopup";
            }
        }

        public override void OnIndex(int? subId = null)
        {
            if (OnCreateAuthCheck())
            {
                PageMenu.Items.Add(new EditorPageMenuItem
                {
                    ItemType = EditPageMenuItemType.Insert,
                    Title = UI.New,
                    IconClass = "fa fa-plus",
                    ButtonClass = "btn btn-default",
                    Url = Url.Action("Create")
                });
            }
            base.OnIndex(subId);
        }

        #region SHOW
        public virtual bool OnShowCheck(T item)
        {
            return true;
        }
        public virtual void OnShowItem(T item)
        {
        }
        public virtual T GetShowItem(int id)
        {
            return Repo.GetItem(id);
        }
        public virtual IActionResult Show(int id)
        {
            if (!OnShowAuthCheck())
                return new ForbidResult();
            var item = GetShowItem(id);
            if (item == null)
                return NotFound();
            if (!OnShowCheck(item))
                return new ForbidResult();

            var _queryDictionary = QueryHelpers.ParseQuery(Request.QueryString.ToString());
            var _queryItems = _queryDictionary.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value)).ToList();
            var gridItem = _queryItems.FirstOrDefault(d => d.Key == "grid");
            var grid = "";
            grid = gridItem.Value;

            ViewBag.ShowMode = true;
            ViewBag.Title = GetTitle(TitleType.Show);
            ViewBag.Grid = grid;
            BuildShowMenu(id);

            OnShowItem(item);

            ViewBag.KeyFieldName = Digger2.GetKeyFieldNameFirst(typeof(T));
            ViewBag.KeyFieldValue = id;

            ViewBag.PageMenu = PageMenu;
            return View(item);
        }
        #endregion

        #region CREATE

        public virtual void AddNewItem(T item)
        {
        }
        public virtual bool OnCreateCheck(T item)
        {
            return true;
        }
        public virtual IActionResult Create()
        {
            if (!OnCreateAuthCheck())
                return new ForbidResult();

            var _queryDictionary = QueryHelpers.ParseQuery(Request.QueryString.ToString());
            var _queryItems = _queryDictionary.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value)).ToList();
            var gridItem = _queryItems.FirstOrDefault(d => d.Key == "grid");
            var grid = "";
            grid = gridItem.Value;

            ViewBag.EditMode = false;
            ViewBag.CreateAction = CreateAction;
            ViewBag.Title = GetTitle(TitleType.Create);
            ViewBag.Grid = grid;
            BuildCreateMenu();

            var item = new T();
            AddNewItem(item);

            if (!OnCreateCheck(item))
                return NotFound();

            ViewBag.PageMenu = PageMenu;
            return View(PopupEditor ? ViewCreatePopup : ViewCreate, item);
        }


        public virtual void OnCreateSaveItem(T item)
        {

        }
        public virtual void OnAfterCreateSaveItem(T item)
        {

        }
        public virtual void OnErrorCreateSaveItem(T item)
        {

        }


        [HttpPost]
        public virtual IActionResult Create(int keepEdit, T item)
        {
            if (!OnCreateAuthCheck())
                return new ForbidResult();

            var keyFieldName = Digger2.GetKeyFieldNameFirst(typeof(T));
            try
            {
                OnCreateSaveItem(item);
                if (Repo.Create(item))
                {
                    OnAfterCreateSaveItem(item);
                    if (keepEdit == 0)
                        return CreateReturnListUrl.IsEmpty() ? (ActionResult)RedirectToAction("Index") : Redirect(CreateReturnListUrl);

                    if (keepEdit == 1)
                    {
                        var itemIdValue = Digger.GetObjectValue(item, keyFieldName);
                        return RedirectToAction("Edit", new { id = itemIdValue });
                    }
                    return CreateSaveAndCreateUrl.IsEmpty() ? (ActionResult)RedirectToAction("Create") : Redirect(CreateSaveAndCreateUrl);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(keyFieldName, MvcLogger.GetErrorMessage(ex));
            }
            ViewBag.EditMode = false;
            ViewBag.CreateAction = CreateAction;
            ViewBag.Title = GetTitle(TitleType.Create);
            BuildCreateMenu();
            OnErrorCreateSaveItem(item);
            ViewBag.PageMenu = PageMenu;
            return View(PopupEditor ? ViewCreatePopup : ViewCreate, item);
        }
        [HttpPost]
        public virtual JsonResult CreatePopup(T item)
        {
            var r = new JsonResultViewModel();
            if (!OnCreateAuthCheck())
            {
                r.Error = "FORBIDDEN";
                return Json(r);
            }
            try
            {
                OnCreateSaveItem(item);
                if (Repo.Create(item))
                {
                    OnAfterCreateSaveItem(item);
                    r.Ok = true;
                    if (!CreateJsAction.IsEmpty())
                        r.JsFunction = CreateJsAction;
                }
                else
                    r.Error = MvcLogger.GetErrorMessage(ModelState);
            }
            catch (Exception ex)
            {
                r.Error = MvcLogger.GetErrorMessage(ex);
            }
            return Json(r);
        }
        #endregion

        #region EDIT
        public virtual bool OnEditCheck(T item)
        {
            return true;
        }
        public virtual void OnEditItem(T item)
        {
        }
        public virtual T GetEditItem(int id)
        {
            return Repo.GetItem(id);
        }
        public virtual IActionResult Edit(int id)
        {
            if (!OnEditAuthCheck())
                return new ForbidResult();

            var item = GetEditItem(id);
            if (item == null)
                return NotFound();

            if (!OnEditCheck(item))
                return NotFound();

            var _queryDictionary = QueryHelpers.ParseQuery(Request.QueryString.ToString());
            var _queryItems = _queryDictionary.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value)).ToList();
            var gridItem = _queryItems.FirstOrDefault(d => d.Key == "grid");
            var grid = "";
            grid = gridItem.Value;

            ViewBag.EditMode = true;
            ViewBag.EditAction = EditAction;
            ViewBag.Title = GetTitle(TitleType.Editor);
            ViewBag.Grid = grid;
            BuildEditMenu(id);

            OnEditItem(item);

            ViewBag.KeyFieldName = Digger2.GetKeyFieldNameFirst(typeof(T));
            ViewBag.KeyFieldValue = id;

            ViewBag.PageMenu = PageMenu;
            return View(PopupEditor ? ViewEditPopup : ViewEdit, item);
        }


        public virtual void OnEditSaveItem(T item)
        {
        }
        public virtual bool OnEditSaveCheck(T item)
        {
            return true;
        }
        public virtual void OnEditBeforeSaveItem(T item, T dbItem)
        {
        }
        public virtual void OnAfterEditSaveItem(T item)
        {
        }
        private int _editId;


        [HttpPost]
        public virtual IActionResult Edit(int keepEdit, T item)
        {
            if (!OnEditAuthCheck())
                return new ForbidResult();

            var keyFieldName = Digger2.GetKeyFieldNameFirst(typeof(T));
            var v = Digger.GetObjectValue(item, keyFieldName);
            if (v != null)
            {
                int.TryParse(v.ToString(), out _editId);
                if (_editId > 0)
                {
                    var org = Repo.GetItem(_editId);
                    OnEditBeforeSaveItem(item, org);
                    TryUpdateModelAsync(org);
                    if (OnEditSaveCheck(org))
                    {
                        OnEditSaveItem(org);
                        try
                        {
                            if (Repo.Edit(org))
                            {
                                OnAfterEditSaveItem(org);
                                if (keepEdit == 0)
                                    return EditReturnListUrl.IsEmpty() ? (ActionResult)RedirectToAction("Index") : Redirect(EditReturnListUrl);
                                return RedirectToAction("Edit", new { id = _editId });
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError(keyFieldName, MvcLogger.GetErrorMessage(ex));
                        }
                        ViewBag.EditMode = true;
                        ViewBag.EditAction = EditAction;
                        ViewBag.Title = GetTitle(TitleType.Editor);
                        BuildEditMenu(_editId);
                        ViewBag.PageMenu = PageMenu;
                        OnEditItem(org);
                        return View(PopupEditor ? ViewEditPopup : ViewEdit, org);
                    }
                    return new ForbidResult();
                }
            }
            return NotFound();
        }
        [HttpPost]
        public virtual JsonResult EditPopup(T item)
        {
            var r = new JsonResultViewModel();
            if (!OnEditAuthCheck())
            {
                r.Error = "FORBIDDEN";
                return Json(r);
            }

            var keyFieldName = Digger2.GetKeyFieldNameFirst(typeof(T));
            var v = Digger.GetObjectValue(item, keyFieldName);
            if (v != null)
            {
                int.TryParse(v.ToString(), out _editId);
                if (_editId > 0)
                {
                    var org = Repo.GetItem(_editId);
                    TryUpdateModelAsync(org);
                    if (OnEditSaveCheck(org))
                    {
                        OnEditSaveItem(org);
                        if (Repo.Edit(org))
                        {
                            r.Ok = true;
                            if (!EditJsAction.IsEmpty())
                                r.JsFunction = EditJsAction;
                        }
                        else
                            r.Error = MvcLogger.GetErrorMessage(ModelState);
                    }
                    r.Error = "ERR";
                }
            }
            else
                r.Error = "NOT FOUND";
            return Json(r);
        }
        #endregion
    }
}
