
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TangkapArena : MonoBehaviour
{
    [Header("AR Foundation")]
    public ARTrackedImageManager imageManager;   // Diisi dari XR Origin

    [Header("Target Arena")]
    public string namaTarget = "ArenaMeja";      // Nama tetap untuk arena
    public float lebarFisikMeter = 0.3f;         // Lebar fisik area bingkai

    [Header("UI")]
    public GameObject uiYangDisembunyikan;       // Root UI, disembunyikan sesaat saat memotret
    public Text teksStatus;                      // Text untuk umpan balik ke pemain

    // Area tangkapan di tengah layar, dalam fraksi ukuran layar.
    // PENTING: samakan dengan ukuran BingkaiPanduan di UI kamu:
    // lebar 70% di tengah, tinggi 50% di tengah.
    const float FraksiLebar = 0.70f;
    const float FraksiTinggi = 0.50f;

    MutableRuntimeReferenceImageLibrary library; // Library yang bisa ditambah saat runtime
    AddReferenceImageJobState jobTerakhir;       // Job penambahan gambar (berjalan async)
    bool sedangMenambahTarget;                   // Penanda agar tidak memproses dua kali

    void Start()
    {
        if (imageManager == null)
        {
            SetStatus("ARTrackedImageManager belum diisi di Inspector.");
            return;
        }

        // Buat library runtime yang kosong. Isinya baru dibuat pemain nanti.
        var runtimeLibrary = imageManager.CreateRuntimeLibrary();

        if (runtimeLibrary is MutableRuntimeReferenceImageLibrary mutable)
        {
            library = mutable;

            // Pasang library baru ke manager (matikan-nyalakan agar diterapkan).
            imageManager.enabled = false;
            imageManager.referenceLibrary = runtimeLibrary;
            imageManager.enabled = true;

            SetStatus("Siap. Arahkan kamera ke meja dan isi bingkai, lalu tekan Jadikan Arena.");
        }
        else
        {
            SetStatus("Perangkat ini tidak mendukung penambahan target saat runtime.");
        }
    }

    // Fungsi ini yang dipanggil tombol "Jadikan Arena".
    public void TangkapDanJadikanArena()
    {
        if (library == null || sedangMenambahTarget) return;
        StartCoroutine(TangkapFrame());
    }

    IEnumerator TangkapFrame()
    {
        // 1) Sembunyikan UI dulu, agar tombol dan bingkai tidak ikut terfoto.
        if (uiYangDisembunyikan != null)
            uiYangDisembunyikan.SetActive(false);

        // 2) Tunggu sampai frame selesai dirender ke layar.
        yield return new WaitForEndOfFrame();

        // 3) Screen capture: baca seluruh tampilan layar (feed kamera AR)
        //    menjadi sebuah Texture2D.
        var tangkapanLayar = new Texture2D(Screen.width, Screen.height,
                                            TextureFormat.RGBA32, false);
        tangkapanLayar.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tangkapanLayar.Apply();

        // 4) UI boleh tampil lagi.
        if (uiYangDisembunyikan != null)
            uiYangDisembunyikan.SetActive(true);

        // 5) Crop: ambil bagian tengah saja, sesuai bingkai panduan.
        int lebarCrop = Mathf.RoundToInt(Screen.width * FraksiLebar);
        int tinggiCrop = Mathf.RoundToInt(Screen.height * FraksiTinggi);
        int xCrop = (Screen.width - lebarCrop) / 2;
        int yCrop = (Screen.height - tinggiCrop) / 2;

        var fotoTarget = new Texture2D(lebarCrop, tinggiCrop,
                                       TextureFormat.RGBA32, false);
        fotoTarget.SetPixels(tangkapanLayar.GetPixels(xCrop, yCrop, lebarCrop, tinggiCrop));
        fotoTarget.Apply();

        Destroy(tangkapanLayar); // Hemat memori, tekstur layar tidak dipakai lagi.

        // 6) Masukkan hasil crop ke Mutable Library.
        TambahkanKeLibrary(fotoTarget);
    }

    void TambahkanKeLibrary(Texture2D fotoTarget)
    {
        // ScheduleAddImageWithValidationJob = minta ARCore/ARKit
        // memvalidasi + mendaftarkan gambar ini sebagai target baru.
        // Prosesnya async (job), hasilnya dipantau di Update().
        jobTerakhir = library.ScheduleAddImageWithValidationJob(
            fotoTarget, namaTarget, lebarFisikMeter);

        sedangMenambahTarget = true;
        SetStatus("Memproses gambar arena...");
    }

    void Update()
    {
        if (!sedangMenambahTarget) return;

        var status = jobTerakhir.status;

        // Di AR Foundation 6 status "sedang diproses" adalah Pending
        // (tidak ada anggota bernama Processing). None = belum terisi.
        if (status == AddReferenceImageJobStatus.Pending ||
            status == AddReferenceImageJobStatus.None)
            return; // Masih diproses, tunggu frame berikutnya.

        sedangMenambahTarget = false;

        if (status == AddReferenceImageJobStatus.Success)
        {
            SetStatus("Arena berhasil dibuat! Arahkan kamera ke area meja tadi.");
        }
        else
        {
            // Contoh penyebab: ErrorInvalidImage -> gambar terlalu polos
            // (meja putih tanpa motif minim titik fitur untuk dilacak).
            SetStatus("Gambar ditolak (" + status + "). " +
                      "Coba area meja yang lebih banyak motif/detail.");
        }
    }

    void SetStatus(string pesan)
    {
        Debug.Log(pesan);
        if (teksStatus != null) teksStatus.text = pesan;
    }
}
