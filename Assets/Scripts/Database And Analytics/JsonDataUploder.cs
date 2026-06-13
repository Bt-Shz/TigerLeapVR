using UnityEngine;
using UnityEngine.UI;
using Firebase.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

public class JSONFileUploader : MonoBehaviour
{
    [Header("UI")]
    public Button uploadButton;

    [Header("Firebase Settings")]
    public string storageBucketUrl = "gs://tigerleap-e950d.firebasestorage.app"
;


    private void Start()
    {
        if (uploadButton != null)
            uploadButton.onClick.AddListener(OnUploadButtonClick);
        else
            Debug.LogWarning("[JSONFileUploader] Upload Button is not assigned!");
    }

    private void OnUploadButtonClick()
    {
        UploadUserFiles();
    }

    private async void UploadUserFiles()
    {
        // Check Firebase initialization
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.isFirebaseInitialized)
        {
            Debug.LogError("[JSONFileUploader] Firebase is not initialized!");
            return;
        }

        // Get current user email
        string email = FirebaseManager.Instance.GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email))
        {
            Debug.LogError("[JSONFileUploader] User not logged in!");
            return;
        }

        // Sanitize email (replace invalid chars)
        string sanitizedEmail = email.Replace("@", "_").Replace(".", "_");

        // Get all files in persistentDataPath
        string[] allFiles = Directory.GetFiles(Application.persistentDataPath);

        // Filter only files that contain this sanitized email
        foreach (string filePath in allFiles)
        {
            string fileName = Path.GetFileName(filePath);

            if (fileName.Contains(sanitizedEmail) &&
                (fileName.StartsWith("PianoTileClickData") || fileName.StartsWith("MohJongHandData")))
            {
                await UploadFile(filePath, sanitizedEmail);
            }
        }
    }

    private async Task UploadFile(string filePath, string sanitizedEmail)
    {
        try
        {
            FirebaseStorage storage = FirebaseStorage.GetInstance(storageBucketUrl);

            // Keep same file name in Firebase
            string storagePath = $"users/{sanitizedEmail}/{Path.GetFileName(filePath)}";
            StorageReference storageRef = storage.GetReference(storagePath);

            Debug.Log($"[JSONFileUploader] Uploading {filePath} to {storageBucketUrl}/{storagePath} ...");

            await storageRef.PutFileAsync(filePath);

            Debug.Log($"[JSONFileUploader] ✅ Successfully uploaded {Path.GetFileName(filePath)} for {sanitizedEmail}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JSONFileUploader] Upload failed for {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }
}
