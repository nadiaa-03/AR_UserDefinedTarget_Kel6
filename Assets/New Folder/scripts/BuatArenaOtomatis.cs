using UnityEngine;

public static class BuatArenaOtomatis
{
    // Warna papan & dinding
    static readonly Color WarnaUbinTerang = new Color(0.93f, 0.87f, 0.73f);
    static readonly Color WarnaUbinGelap  = new Color(0.45f, 0.30f, 0.18f);
    static readonly Color WarnaDinding    = new Color(0.35f, 0.65f, 0.75f);
    static readonly Color WarnaPion       = new Color(0.85f, 0.25f, 0.25f);
    static readonly Color WarnaFinish     = new Color(0.25f, 0.80f, 0.35f);

    public static GameObject Bangun()
    {
        var akar = new GameObject("ArenaOtomatis");

        BuatPapanCatur(akar.transform);
        BuatDindingLabirin(akar.transform);
        BuatPionDanFinish(akar.transform);

        return akar;
    }

    // Papan 8 x 8 dari kubus tipis, warna selang-seling seperti catur.
    static void BuatPapanCatur(Transform induk)
    {
        var matTerang = BuatMaterial(WarnaUbinTerang);
        var matGelap  = BuatMaterial(WarnaUbinGelap);

        const int jumlah = 8;
        float ukuranUbin = 1f / jumlah;              // 0.125

        for (int x = 0; x < jumlah; x++)
        {
            for (int z = 0; z < jumlah; z++)
            {
                var ubin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ubin.name = "Ubin_" + x + "_" + z;
                ubin.transform.SetParent(induk, false);
                ubin.transform.localPosition = new Vector3(
                    -0.5f + ukuranUbin * (x + 0.5f),
                    0.011f,
                    -0.5f + ukuranUbin * (z + 0.5f));
                ubin.transform.localScale = new Vector3(
                    ukuranUbin * 0.98f, 0.022f, ukuranUbin * 0.98f);
                ubin.GetComponent<Renderer>().sharedMaterial =
                    (x + z) % 2 == 0 ? matTerang : matGelap;

                // Collider ubin tidak dibutuhkan; matikan agar ringan.
                Object.Destroy(ubin.GetComponent<Collider>());
            }
        }
    }

    // Dinding labirin: tiap baris = (posX, posZ, panjangX, panjangZ).
    // Tinggi dinding 0.09, berdiri di atas papan (permukaan papan y=0.022).
    static void BuatDindingLabirin(Transform induk)
    {
        var matDinding = BuatMaterial(WarnaDinding);

        float[,] dinding =
        {
            // Dinding keliling (menyisakan celah masuk di kiri bawah)
            {  0.00f,  0.475f, 1.00f, 0.05f },
            {  0.00f, -0.475f, 1.00f, 0.05f },
            {  0.475f,  0.00f, 0.05f, 1.00f },
            { -0.475f,  0.25f, 0.05f, 0.50f },
            // Dinding dalam (jalur labirin sederhana)
            { -0.25f,  0.25f, 0.45f, 0.05f },
            {  0.25f,  0.25f, 0.45f, 0.05f },
            { -0.25f, -0.25f, 0.45f, 0.05f },
            {  0.10f, -0.25f, 0.30f, 0.05f },
            {  0.00f,  0.00f, 0.05f, 0.45f },
            { -0.35f,  0.00f, 0.25f, 0.05f },
            {  0.35f, -0.05f, 0.05f, 0.35f },
        };

        for (int i = 0; i < dinding.GetLength(0); i++)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cube);
            d.name = "Dinding_" + i;
            d.transform.SetParent(induk, false);
            d.transform.localPosition = new Vector3(
                dinding[i, 0], 0.022f + 0.045f, dinding[i, 1]);
            d.transform.localScale = new Vector3(
                dinding[i, 2], 0.09f, dinding[i, 3]);
            d.GetComponent<Renderer>().sharedMaterial = matDinding;
            Object.Destroy(d.GetComponent<Collider>());
        }
    }

    // Pion bola merah di sudut start + silinder hijau sebagai finish.
    static void BuatPionDanFinish(Transform induk)
    {
        var pion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pion.name = "PionPemain";
        pion.transform.SetParent(induk, false);
        pion.transform.localPosition = new Vector3(-0.4375f, 0.057f, -0.4375f);
        pion.transform.localScale = Vector3.one * 0.07f;
        pion.GetComponent<Renderer>().sharedMaterial = BuatMaterial(WarnaPion);
        Object.Destroy(pion.GetComponent<Collider>());

        var finish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        finish.name = "TitikFinish";
        finish.transform.SetParent(induk, false);
        finish.transform.localPosition = new Vector3(0.4375f, 0.032f, 0.4375f);
        finish.transform.localScale = new Vector3(0.09f, 0.01f, 0.09f);
        finish.GetComponent<Renderer>().sharedMaterial = BuatMaterial(WarnaFinish);
        Object.Destroy(finish.GetComponent<Collider>());
    }

    // Material berwarna polos. Memakai shader Lit URP bila proyeknya URP,
    // dan Standard bila Built-in Render Pipeline.
    static Material BuatMaterial(Color warna)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.color = warna;
        return mat;
    }
}
