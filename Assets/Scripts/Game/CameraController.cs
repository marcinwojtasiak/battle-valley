using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraController : MonoBehaviour
{
    public Tilemap tilemap;
    public Camera mainCamera;
    public float xCameraSpeed;
    public float yCameraSpeed;
    public int xMargin;
    public int yMargin;
    public int xBorderOffset;
    public int yBorderOffset;
    public float scrollSpeed;
    public float scrollMin;
    public float scrollMax;

    private float mapWidth;
    private float mapHeight;

    public void EnableCameraControlls(Tilemap tilemap)
    {
        enabled = true;
        this.tilemap = tilemap;
    }

    public void DisableCameraControlls()
    {
        enabled = false;
    }

    private void Update()
    {
        if (tilemap == null)
            return;

        if (Input.mouseScrollDelta.y != 0)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                mainCamera.orthographicSize -= Input.mouseScrollDelta.y * scrollSpeed * Time.deltaTime;
                mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, scrollMin, scrollMax);
            }
        }

        Vector3 mousePosition = Input.mousePosition;

        Vector3 mouseView = mainCamera.ScreenToViewportPoint(mousePosition);
        if (mouseView.x < 0 || mouseView.x > 1 || mouseView.y < 0 || mouseView.y > 1)
            return;

        float xTranslation = 0f;
        float yTranslation = 0f;

        Vector3 cameraMinWorld = mainCamera.ScreenToWorldPoint(mainCamera.pixelRect.min);
        Vector3 cameraMaxWorld = mainCamera.ScreenToWorldPoint(mainCamera.pixelRect.max);

        if (mousePosition.x >= mainCamera.scaledPixelWidth - xMargin)
        {
            mapWidth = tilemap.size.x * tilemap.cellSize.x;
            if (cameraMaxWorld.x < tilemap.transform.position.x + mapWidth + xBorderOffset)
            {
                xTranslation = xCameraSpeed * Time.deltaTime;
            }
        }

        if (mousePosition.x <= xMargin)
        {
            if (cameraMinWorld.x > tilemap.transform.position.x - xBorderOffset)
            {
                xTranslation = -xCameraSpeed * Time.deltaTime;
            }
        }

        if (mousePosition.y >= mainCamera.scaledPixelHeight - yMargin)
        {
            mapHeight = tilemap.size.y * tilemap.cellSize.y;
            if (cameraMaxWorld.y < tilemap.transform.position.y + mapHeight + yBorderOffset)
            {
                yTranslation = yCameraSpeed * Time.deltaTime;
            }
        }

        if (mousePosition.y <= yMargin)
        {
            if (cameraMinWorld.y > tilemap.transform.position.y - yBorderOffset)
            {
                yTranslation = -yCameraSpeed * Time.deltaTime;
            }
        }

        mainCamera.transform.Translate(new Vector3(xTranslation, yTranslation, 0f));
    }
}
