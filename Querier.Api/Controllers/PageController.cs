using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Querier.Api.Application.DTOs.Requests.Page;
using Querier.Api.Application.DTOs.Responses.Page;
using Querier.Api.Application.Interfaces.Services.Menu;

namespace Querier.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PageController : ControllerBase
    {
        private readonly IDynamicPageService _pageService;

        public PageController(IDynamicPageService pageService)
        {
            _pageService = pageService;
        }

        /// <summary>
        /// Récupère toutes les pages d'une catégorie de menu
        /// </summary>
        /// <param name="categoryId">ID de la catégorie</param>
        /// <returns>Liste des pages</returns>
        /// <response code="200">Retourne la liste des pages</response>
        /// <response code="404">Catégorie non trouvée</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PageResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<PageResponse>>> GetAll([FromQuery] int categoryId)
        {
            return Ok(await _pageService.GetAllAsync());
        }

        /// <summary>
        /// Récupère une page par son ID
        /// </summary>
        /// <param name="id">ID de la page</param>
        /// <returns>La page demandée</returns>
        /// <response code="200">Retourne la page demandée</response>
        /// <response code="404">Page non trouvée</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PageResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PageResponse>> GetById(int id)
        {
            var page = await _pageService.GetByIdAsync(id);
            if (page == null) return NotFound();
            return Ok(page);
        }

        /// <summary>
        /// Crée une nouvelle page
        /// </summary>
        /// <param name="request">Données de la page à créer</param>
        /// <returns>La page créée</returns>
        /// <response code="201">Retourne la page créée</response>
        /// <response code="400">Requête invalide</response>
        [HttpPost]
        [ProducesResponseType(typeof(PageResponse), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PageResponse>> Create(CreatePageRequest request)
        {
            var result = await _pageService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Met à jour une page existante
        /// </summary>
        /// <param name="id">ID de la page</param>
        /// <param name="request">Nouvelles données de la page</param>
        /// <returns>La page mise à jour</returns>
        /// <response code="200">Retourne la page mise à jour</response>
        /// <response code="404">Page non trouvée</response>
        /// <response code="400">Requête invalide</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PageResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PageResponse>> Update(int id, CreatePageRequest request)
        {
            var result = await _pageService.UpdateAsync(id, request);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Supprime une page
        /// </summary>
        /// <param name="id">ID de la page à supprimer</param>
        /// <returns>Aucun contenu</returns>
        /// <response code="204">Page supprimée avec succès</response>
        /// <response code="404">Page non trouvée</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _pageService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
} 