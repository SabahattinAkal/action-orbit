using System.Net.Sockets;

namespace ActionOrbit.App.Services;

public static class OrbitLinkConnectionMessage
{
    public static string FromException(Exception exception)
    {
        var cause = exception.GetBaseException();
        if (cause is OperationCanceledException or TimeoutException)
            return "Cihaz zamanında yanıt vermedi. Diğer bilgisayarda Orbit Link'in açık olduğunu ve VPN bağlantısını kontrol et.";
        if (cause is SocketException socket)
            return socket.SocketErrorCode switch
            {
                SocketError.ConnectionRefused => "Hedef bağlantıyı kabul etmedi. Diğer bilgisayarda Action Orbit'i aç ve Orbit Link'i etkinleştir.",
                SocketError.TimedOut => "Cihaz zamanında yanıt vermedi. İki bilgisayarın aynı ağda veya VPN'de olduğunu ve uygulamaya ağ izni verildiğini kontrol et.",
                SocketError.NetworkUnreachable or SocketError.HostUnreachable => "Hedef ağa ulaşılamıyor. Ağ veya VPN bağlantısını kontrol et.",
                SocketError.HostNotFound or SocketError.NoData => "Cihaz adresi bulunamadı. Diğer bilgisayarda gösterilen güncel adresi kullan.",
                SocketError.AccessDenied => "Ağ erişimi engellendi. Kurumsal bir bilgisayarsa uygulama izni için sistem yöneticine danış.",
                SocketError.AddressAlreadyInUse => "Orbit Link bağlantı noktası kullanımda. Açık başka bir Action Orbit kopyası olup olmadığını kontrol et.",
                _ => "Cihazla bağlantı kurulamadı. Adresi, Orbit Link durumunu ve ağ bağlantısını kontrol edip tekrar dene."
            };
        // Exception text can include addresses, user paths or protocol details; don't display it.
        return "Bağlantı tamamlanamadı. Cihaz adresini kontrol edip yeni bir eşleştirme koduyla tekrar dene.";
    }
}
