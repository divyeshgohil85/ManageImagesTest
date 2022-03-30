using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ICSharpCode.SharpZipLib.Zip;
using ManageImageTest.Models;

namespace ManageImageTest.Controllers
{
    public class ImageController : Controller
    {
        public readonly ImagesContext _context;
        public ActionResult Index()
        {
            using (var db = new ImagesContext())
            {
                List<Image> images = db.Images.ToList();
                return View(images);
            }
        }
        public ActionResult AddImage()
        {
            return View();
        }


        #region Function to save data
        [HttpPost]
        public ActionResult AddImage(string title)
        {
            Image img = new Image();
            if (Request != null && Request.Files != null && Request.Files.Count != 0)
            {
                var file = Request.Files[0];
                byte[] fileInBytes = new byte[file.ContentLength];
                using (BinaryReader theReader = new BinaryReader(file.InputStream))
                {
                    fileInBytes = theReader.ReadBytes(file.ContentLength);
                }
                img.ImageName = file.FileName;
                img.ImageText = Convert.ToBase64String(fileInBytes);
                img.Title = title;
                img.DateConverted = DateTime.UtcNow;
                using (var db = new ImagesContext())
                {
                    db.Images.Add(img);
                    db.SaveChanges();
                }
                return Json("File Uploaded Successfully!");
                //return RedirectToAction("Index");
            }
            return View();
        }
        #endregion

        #region Function to download zip file
        public FileResult DownloadZipFile(string imageIds)
        {
            imageIds = imageIds.TrimEnd(',');
            string[] ids = imageIds.Split(',');
            var fileName = string.Format("{0}_ImageFiles.zip", DateTime.Today.Date.ToString("dd-MM-yyyy") + "_1");
            var tempOutPutPath = Server.MapPath(Url.Content("/TempImages/")) + fileName;

            using (ZipOutputStream currentZip = new ZipOutputStream(System.IO.File.Create(tempOutPutPath)))
            {
                currentZip.SetLevel(9);
                byte[] buffer = new byte[4096];

                foreach (string id in ids)
                {
                    using (var db = new ImagesContext())
                    {
                        try
                        {
                            int intId = Convert.ToInt32(id);

                            Image image = db.Images.Find(intId);
                            if (image != null && !string.IsNullOrEmpty(image.ImageText))
                            {
                                ZipEntry entry = new ZipEntry(image.ImageName);
                                entry.DateTime = DateTime.Now;
                                entry.IsUnicodeText = true;
                                currentZip.PutNextEntry(entry);
                                byte[] file = Convert.FromBase64String(image.ImageText);
                                using (var stream = new MemoryStream(file))
                                {
                                    int sourceBytes;
                                    do
                                    {
                                        sourceBytes = stream.Read(buffer, 0, buffer.Length);
                                        currentZip.Write(buffer, 0, sourceBytes);
                                    } while (sourceBytes > 0);
                                }
                            }
                        }
                        catch (Exception ex) { continue; }
                    }
                }
                currentZip.Finish();
                currentZip.Flush();
                currentZip.Close();
            }
            byte[] finalResult = System.IO.File.ReadAllBytes(tempOutPutPath);

            //Check and delete temp file
            if (System.IO.File.Exists(tempOutPutPath))
            {
                System.IO.File.Delete(tempOutPutPath);
            }
            

            if (finalResult == null || !finalResult.Any())
                throw new Exception(String.Format("No Files found with Image"));

            return File(finalResult, "application/zip", fileName);
        }
        #endregion

        //#region Function to delete file
        //public ActionResult Delete(int id)
        //{
        //    Image image = _context.Images.FirstOrDefault(m => m.ImageId == id);
        //    if (id > 0)
        //    {
        //        _context.Images.Remove(image);
        //        _context.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    return View();
        //}
        //#endregion
    }
}