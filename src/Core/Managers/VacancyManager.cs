﻿using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
using System;
using System.Collections.Generic;

namespace Core.Managers
{
    public interface IVacancyManager
    {
        Task<IPagedList<DAL.Vacancy>> GetVacancies(int page = 1, int pageSize = 10);
        IList<DAL.Vacancy> GetHotVacancies();
        Task<DAL.Vacancy> Get(int id);
        Task<Vacancy> Save(Vacancy vacancy);
        Task IncreaseViewCount(int id);
    }

    public class VacancyManager : ManagerBase, IVacancyManager
    {
        private readonly DAL.DatabaseContext _database;

        public VacancyManager(IMemoryCache cache, DatabaseContext database)
            : base(cache)
        {
            _database = database;
        }

        public async Task<IPagedList<DAL.Vacancy>> GetVacancies(int page = 1, int pageSize = 10)
        {
            var key = $"vacancy{page}_{pageSize}";

            var result = _cache.Get(key) as IPagedList<DAL.Vacancy>;

            if (result == null)
            {
                var skip = (page - 1) * pageSize;

                var items = _database
                    .Vacancy
                    .Include(o => o.Category)
                    .Where(o => o.Active && o.LanguageId == Core.Language.EnglishId)
                    .OrderByDescending(o => o.Id)
                    .Skip(skip)
                    .Take(pageSize).ToList();

                var totalItemsCount = await _database.Vacancy.Where(o => o.Active).CountAsync();

                result = new StaticPagedList<Vacancy>(items, page, pageSize, totalItemsCount);
                _cache.Set(key, result, GetMemoryCacheEntryOptions());
            }

            return result;
        }

        public IList<DAL.Vacancy> GetHotVacancies()
        {
            var key = $"hot_vacancies";
            var size = 5;

            var result = _cache.Get(key) as IList<DAL.Vacancy>;

            if (result == null)
            {
                result = _database
                    .Vacancy
                    .Include(o => o.Category)
                    .Where(o => o.Active && o.LanguageId == Core.Language.EnglishId)
                    .OrderByDescending(o => o.Id)
                    .Take(size).ToList();

                _cache.Set(key, result, GetMemoryCacheEntryOptions());
            }

            return result;
        }

        public async Task<DAL.Vacancy> Get(int id)
        {
            var key = $"vacancy_{id}";

            var result = _cache.Get(key) as Vacancy;
            result = null;

            if (result == null)
            {
                result = _database.Vacancy.SingleOrDefault(o => o.Id == id);
                _cache.Set(key, result, GetMemoryCacheEntryOptions());
            }

            return await Task.FromResult(result);
        }

        public async Task<Vacancy> Save(Vacancy vacancy)
        {
            _database.Add(vacancy);
            await _database.SaveChangesAsync();
            vacancy = _database.Vacancy.LastOrDefault();

            return vacancy;
        }

        public async Task IncreaseViewCount(int id)
        {
            var vacancy = _database.Vacancy.SingleOrDefault(o => o.Id == id);
            
            if (vacancy != null)
            {
                vacancy.Views++;
                await _database.SaveChangesAsync();
            }
        }
    }
}
