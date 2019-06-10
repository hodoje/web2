﻿using AutoMapper;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using WebApp.Models.DomainModels;
using WebApp.Models.Dtos;
using WebApp.Persistence.UnitOfWork;

namespace WebApp.Controllers.DomainControllers
{
    public class TransportationLinesController : ApiController
    {
		private IUnitOfWork unitOfWork;
		private IMapper mapper;

		public TransportationLinesController(IUnitOfWork uow, IMapper imapper)
		{
			unitOfWork = uow;
			mapper = imapper;
		}

		// GET: api/TransportationLines
		public IEnumerable<TransportationLineDto> GetTransportationLines()
        {
			return mapper.Map<List<TransportationLine>, List<TransportationLineDto>>(unitOfWork.TransportationLineRepository.GetAll().ToList());
		}

        // GET: api/TransportationLines/5
        [ResponseType(typeof(TransportationLine))]
        public IHttpActionResult GetTransportationLine(int id)
        {
            TransportationLine transportationLine = unitOfWork.TransportationLineRepository.Get(id);
            if (transportationLine == null)
            {
                return NotFound();
            }

            return Ok(transportationLine);
        }

        // AUTHORIZATION !?
        // GET: api/TransporationLines/GetPlan/5
        public IHttpActionResult GetPlanForTransporationLine(int lineNum)
        {
            try
            {
                TransportationLine transportationLine = unitOfWork.TransportationLineRepository.Find(x => x.LineNum == lineNum).FirstOrDefault();

                if (transportationLine == null)
                {
                    return null;
                }

                TransporationLinePlanDto planDto = new TransporationLinePlanDto() { LineNumber = transportationLine.LineNum, Routes = new List<RoutePointDto>() };

                List<TransportationLineRoute> routes = unitOfWork.TransportationLineRouteRepository.Find(x => x.TransporationLineId == transportationLine.Id).ToList();
                routes.Sort(TransportationLineRoute.CompareByRoutePoint);

                foreach (TransportationLineRoute route in routes)
                {
                    planDto.Routes.Add(new RoutePointDto()
                    {
                        SequenceNumber = route.RoutePoint,
                        Station = unitOfWork.StationRepository.Get(route.StationId)
                    });
                }

                return Ok(planDto);
            }
            catch
            {
                return BadRequest();
            }
        }

        // PUT: api/TransportationLines/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutTransportationLine(int id, TransportationLine transportationLine)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != transportationLine.Id)
            {
                return BadRequest();
            }

            try
            {
				unitOfWork.TransportationLineRepository.Update(transportationLine);
				unitOfWork.Complete();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransportationLineExists(id))
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

        // POST: api/TransportationLines
        [ResponseType(typeof(TransportationLine))]
        public IHttpActionResult PostTransportationLine(TransportationLine transportationLine)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

			unitOfWork.TransportationLineRepository.Add(transportationLine);
			unitOfWork.Complete();

            return CreatedAtRoute("DefaultApi", new { id = transportationLine.Id }, transportationLine);
        }

        // DELETE: api/TransportationLines/5
        [ResponseType(typeof(TransportationLine))]
        public IHttpActionResult DeleteTransportationLine(int id)
        {
            TransportationLine transportationLine = unitOfWork.TransportationLineRepository.Get(id);
            if (transportationLine == null)
            {
                return NotFound();
            }

			unitOfWork.TransportationLineRepository.Remove(transportationLine);
			unitOfWork.Complete();

            return Ok(transportationLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                unitOfWork.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool TransportationLineExists(int id)
        {
            return unitOfWork.TransportationLineRepository.Get(id) != null;
        }
    }
}