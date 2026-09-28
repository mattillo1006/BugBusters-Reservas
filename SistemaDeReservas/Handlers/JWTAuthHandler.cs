using System.Net.Http.Headers;

namespace SistemaDeReservas.Handlers
{
    // Este archivo o Handler funciona como middleware para agregar el token JWT a las solicitudes HTTP salientes hacia la WebAPI.
    public class JWTAuthHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JWTAuthHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Se busca el valor que tiene el token, pensando en que ya se inicio Sesion
            var token = _httpContextAccessor.HttpContext?.User
                .FindFirst("AccessToken")?.Value;

            // Verifica que el valor del token no sea nulo o vacío, y si es así, agrega el encabezado de autorización a la solicitud HTTP.
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Devuelve la solicitud para que continue su viaje, ya con el token agregado en el encabezado de autorización.
            return base.SendAsync(request, cancellationToken);
        }
    }
}
