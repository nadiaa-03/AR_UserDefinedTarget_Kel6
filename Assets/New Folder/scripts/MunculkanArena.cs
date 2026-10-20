
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MunculkanArena : MonoBehaviour
{
    [Header("AR Foundation")]
    public ARTrackedImageManager imageManager;

    [Header("Arena")]
    [Tooltip("Hanya target dengan nama ini yang diberi arena.")]
    public string namaTargetArena = "ArenaMeja";
    [Tooltip("Boleh dikosongkan: kalau kosong, arena dibangun otomatis " +
             "dari kode oleh BuatArenaOtomatis.cs (papan catur + labirin).")]
    public GameObject prefabArena;

    // Arena yang sedang tampil, dicatat per gambar yang terdeteksi.
    readonly Dictionary<TrackableId, GameObject> arenaAktif =
        new Dictionary<TrackableId, GameObject>();

    void OnEnable()
    {
        // trackablesChanged = event AR Foundation 6 yang memberi tahu
        // setiap ada gambar ditambahkan / diperbarui / hilang dari tracking.
        if (imageManager != null)
            imageManager.trackablesChanged.AddListener(OnGambarBerubah);
    }

    void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.RemoveListener(OnGambarBerubah);
    }

    void OnGambarBerubah(ARTrackablesChangedEventArgs<ARTrackedImage> e)
    {
        foreach (var tracked in e.added)              // Baru terdeteksi
            TampilkanArena(tracked);

        foreach (var tracked in e.updated)            // Status tracking berubah
        {
            if (tracked.trackingState == TrackingState.Tracking)
                TampilkanArena(tracked);
            else
                SembunyikanArena(tracked.trackableId); // Tracking hilang -> arena hilang
        }

        foreach (var pasangan in e.removed)           // Gambar dihapus dari library
            SembunyikanArena(pasangan.Key);
    }

    void TampilkanArena(ARTrackedImage tracked)
    {
        if (arenaAktif.ContainsKey(tracked.trackableId))
            return; // Arena sudah ada, jangan dobel.

        // Hanya target buatan pemain yang diberi arena. Gambar lain
        // (misalnya gambar "Cadangan" di LibraryAwal) diabaikan.
        if (tracked.referenceImage.name != namaTargetArena)
            return;

        GameObject arena;

        if (prefabArena != null)
        {
            // Instantiate sebagai ANAK dari tracked image: arena otomatis
            // menempel dan mengikuti gerakan/rotasi gambar fisik di meja.
            arena = Instantiate(prefabArena, tracked.transform);
        }
        else
        {
            // Tanpa prefab: arena dibangun otomatis dari kode.
            arena = BuatArenaOtomatis.Bangun();
            arena.transform.SetParent(tracked.transform, false);
        }
        arena.transform.localPosition = Vector3.zero;
        arena.transform.localRotation = Quaternion.identity;

        // Prefab dibangun 1 x 1 satuan, jadi skalakan ke ukuran fisik
        // target (tracked.size dalam meter: x = lebar, y = tinggi gambar).
        arena.transform.localScale = new Vector3(
            tracked.size.x, 1f, tracked.size.y);

        arenaAktif[tracked.trackableId] = arena;
        Debug.Log("Arena ditampilkan di atas target '" +
                  tracked.referenceImage.name + "'.");
    }

    void SembunyikanArena(TrackableId id)
    {
        if (arenaAktif.TryGetValue(id, out var arena))
        {
            if (arena != null) Destroy(arena);
            arenaAktif.Remove(id);
        }
    }
}
