﻿using AutoMapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using WebApp.BusinessComponents;
using WebApp.Models.DomainModels;
using WebApp.Models.Dtos;
using WebApp.Persistence;
using WebApp.Persistence.UnitOfWork;

namespace WebApp.Controllers.DomainControllers
{
    public class PricelistsController : ApiController
    {
		private IUnitOfWork unitOfWork;
		private IMapper mapper;
		private IPricelistComponent pricelistComponent;

		public PricelistsController(IUnitOfWork uow, IMapper imapper, IPricelistComponent plComponent)
		{
			unitOfWork = uow;
			mapper = imapper;
			pricelistComponent = plComponent;
		}

		// GET: api/Pricelists
		public IHttpActionResult GetPricelists()
		{
			try
			{
				List<Pricelist> pricelists = unitOfWork.PricelistRepository.GetAll().ToList();
				List<PricelistDto> plDtos = new List<PricelistDto>(pricelists.Count);
				foreach (var pricelist in pricelists)
				{
					List<TicketTypePricelist> ttpls = unitOfWork.TicketTypePricelistRepository.FindIncludeTicketType(ttpl => ttpl.PricelistId == pricelist.Id).ToList();
					PricelistDto pricelistDto = new PricelistDto() { FromDate = pricelist.FromDate };
					foreach (TicketTypePricelist ttpl in ttpls)
					{
						pricelistComponent.SetPricelistPriceFromTicketType(ref pricelistDto, ttpl);
					}
					plDtos.Add(pricelistDto);
				}
				if (plDtos.Count > 0)
				{
					return Ok(plDtos);
				}

				return NotFound();
			}
			catch (Exception e)
			{
				return InternalServerError();
			}
		}

		// GET: api/Pricelists/5
		[ResponseType(typeof(Pricelist))]
        public IHttpActionResult GetPricelist(int id)
        {
			Pricelist pricelist = unitOfWork.PricelistRepository.Get(id);
            if (pricelist == null)
            {
                return NotFound();
            }

            return Ok(pricelist);
        }

		[HttpGet]
		[Route("api/pricelists/getActivePricelist")]
		public IHttpActionResult GetLatestPricelist()
		{
			Pricelist pricelist = unitOfWork.PricelistRepository.GetActivePricelist();
			PricelistDto pricelistDto = mapper.Map<Pricelist, PricelistDto>(pricelist);
			List<TicketTypePricelist> ttpls = unitOfWork.TicketTypePricelistRepository.FindIncludeTicketType(ttpl => ttpl.PricelistId == pricelist.Id).ToList();
			foreach(TicketTypePricelist ttpl in ttpls)
			{
				pricelistComponent.SetPricelistPriceFromTicketType(ref pricelistDto, ttpl);
			}
			if (pricelist == null)
			{
				return NotFound();
			}
			return Ok(pricelistDto);
		}

        // PUT: api/Pricelists/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutPricelist(int id, Pricelist pricelist)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != pricelist.Id)
            {
                return BadRequest();
            }

            try
            {
				unitOfWork.PricelistRepository.Update(pricelist);
				unitOfWork.Complete();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PricelistExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Pricelists
        [ResponseType(typeof(PricelistDto))]
        public IHttpActionResult PostPricelist(PricelistDto pricelistDto)
        {
			Pricelist pricelist = mapper.Map<PricelistDto, Pricelist>(pricelistDto);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

			unitOfWork.PricelistRepository.Add(pricelist);
			unitOfWork.Complete();

			foreach(TicketType tt in unitOfWork.TicketTypeRepository.GetAll())
			{
				TicketTypePricelist ttpl = new TicketTypePricelist(tt.Id, pricelist.Id);
				pricelistComponent.SetTicketTypeBasePriceFromPricelist(tt, ref ttpl, pricelistDto);
				//if(tt.Name == "Hourly")
				//{
				//	ttpl.BasePrice = pricelistDto.Hourly;
				//}
				//else if(tt.Name == "Daily")
				//{
				//	ttpl.BasePrice = pricelistDto.Daily;
				//}
				//else if(tt.Name == "Monthly")
				//{
				//	ttpl.BasePrice = pricelistDto.Monthly;
				//}
				//else if(tt.Name == "Yearly")
				//{
				//	ttpl.BasePrice = pricelistDto.Yearly;
				//}
				unitOfWork.TicketTypePricelistRepository.Add(ttpl);
			}
			unitOfWork.Complete();

            return CreatedAtRoute("DefaultApi", new { id = pricelist.Id }, pricelist);
        }

        // DELETE: api/Pricelists/5
        [ResponseType(typeof(Pricelist))]
        public IHttpActionResult DeletePricelist(int id)
        {
            Pricelist pricelist = unitOfWork.PricelistRepository.Get(id);
            if (pricelist == null)
            {
                return NotFound();
            }

			unitOfWork.PricelistRepository.Remove(pricelist);
			unitOfWork.Complete();

            return Ok(pricelist);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
				unitOfWork.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool PricelistExists(int id)
        {
            return unitOfWork.PricelistRepository.Get(id) != null;
        }
    }
}