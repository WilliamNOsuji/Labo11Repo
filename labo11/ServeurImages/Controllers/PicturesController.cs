using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServeurImages.Data;
using ServeurImages.Models;
using SixLabors.ImageSharp.Processing;
using NuGet.Protocol;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace ServeurImages.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PicturesController : ControllerBase
    {
        private readonly ServeurImagesContext _context;

        public PicturesController(ServeurImagesContext context)
        {
            _context = context;
        }

        // GET: api/Pictures
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Picture>>> GetPicture()
        {
          if (_context.Picture == null)
          {
              return NotFound();
          }
            return await _context.Picture.ToListAsync();
        }

        // GET: api/Pictures/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Picture>> GetFile(string size,int id)
        {
            if (_context.Picture == null)
            {
                return NotFound();
            }
            Picture? picture = await _context.Picture.FindAsync(id);
            if(picture == null || picture.FileName == null || picture.MimeType == null)
            {
                return NotFound(new { Message = "Cette photo n'existe pas ou n'a pas de photo." });
            }
            if(!Regex.Match(size, "lg|sm").Success)
            {
                return BadRequest(new { Message = "La taille demandée est inadéquate." });
            }
            byte[] bytes = System.IO.File.ReadAllBytes(Directory.GetCurrentDirectory() + "/images/" + size + "/" + picture.FileName);
            return File(bytes, picture.MimeType);
        }

        // POST: api/Pictures
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<Picture>> PostPicture()
        {
            try
            {
                IFormCollection formCollection = await Request.ReadFormAsync();
                IFormFile? file = formCollection.Files.GetFile("monImage");
                if (file != null)
                {
                    Picture picture = new Picture(); // Create a new Picture object

                    Image image = Image.Load(file.OpenReadStream());

                    picture.FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    picture.MimeType = file.ContentType;

                    image.Save(Directory.GetCurrentDirectory() + "/images/lg/" + picture.FileName);

                    image.Mutate(i =>
                        i.Resize(new ResizeOptions()
                        {
                            Mode = ResizeMode.Min,
                            Size = new Size() { Width = 320 }
                        })
                    );
                    image.Save(Directory.GetCurrentDirectory() + "/images/sm/" + picture.FileName);

                    // Add the picture to the database
                    await _context.Picture.AddAsync(picture);
                    await _context.SaveChangesAsync();

                    return Ok(picture);
                }
                else
                {
                    return NotFound(new { Message = "Aucune image fournie" });
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return StatusCode(500, new { Message = "Une erreur s'est produite lors de l'ajout de l'image." });
            }
        }


        // DELETE: api/Pictures/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePicture(int id)
        {
            if (_context.Picture == null)
            {
                return NotFound();
            }
            var picture = await _context.Picture.FindAsync(id);
            if (picture == null)
            {
                return NotFound(new { Message = "Cette photo n'existe pas." });
            }

            if(picture.MimeType != null && picture.FileName !=null)
            {
                System.IO.File.Delete(Directory.GetCurrentDirectory() + "/images/lg/" + picture.FileName);
                System.IO.File.Delete(Directory.GetCurrentDirectory() + "/images/sm/" + picture.FileName);
            }

            _context.Picture.Remove(picture);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PictureExists(int id)
        {
            return (_context.Picture?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
