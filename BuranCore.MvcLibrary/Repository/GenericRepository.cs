﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Buran.Core.MvcLibrary.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly IValidationDictionary ValidationDictionary;
        protected DbContext Context;
        protected DbSet<T> DbSet;

        public GenericRepository(IValidationDictionary validationDictionary, DbContext context)
        {
            ValidationDictionary = validationDictionary;
            Context = context;
            if (context != null)
            {
                DbSet = context.Set<T>();
            }
        }

        public virtual IQueryable<T> GetList(Expression<Func<T, bool>> filter = null)
        {
            IQueryable<T> query = DbSet.AsNoTracking();
            if (filter != null)
                query = query.Where(filter);
            return query;
        }

        public virtual T GetItem(int id)
        {
            return DbSet.Find(id);
        }

        protected void RaiseError(string error, string field)
        {
            if (ValidationDictionary != null)
                ValidationDictionary.AddError(field, error);
            else
                throw new Exception(error);
        }
        public virtual bool ValidateItem(T item)
        {
            return ValidationDictionary == null || ValidationDictionary.IsValid;
        }
        public virtual bool ValidateDeleteItem(T item)
        {
            return ValidationDictionary == null || ValidationDictionary.IsValid;
        }

        public virtual bool Create(T item)
        {
            if (!ValidateItem(item))
                return false;
            DbSet.Add(item);
            Context.SaveChanges();
            return true;
        }

        public virtual bool Edit(T item)
        {
            if (!ValidateItem(item))
                return false;
            Context.Entry(item).State = EntityState.Modified;
            Context.SaveChanges();
            return true;
        }

        public virtual bool Delete(object id)
        {
            T entityToDelete = DbSet.Find(id);
            return Delete(entityToDelete);
        }

        public virtual bool Delete(T item)
        {
            if (!ValidateDeleteItem(item))
                return false;
            if (Context.Entry(item).State == EntityState.Detached)
                DbSet.Attach(item);
            DbSet.Remove(item);
            Context.SaveChanges();
            return true;
        }
    }
}