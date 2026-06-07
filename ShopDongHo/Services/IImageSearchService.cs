using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopDongHo.Services
{
    public interface IImageSearchService
    {
        /// <summary>
        /// Extract Vietnamese keywords from image via local vision model (Ollama).
        /// </summary>
        Task<List<string>> ExtractKeywordsFromImageAsync(IFormFile image);
    }
}