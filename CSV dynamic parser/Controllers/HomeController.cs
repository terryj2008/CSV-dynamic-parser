using CSV_dynamic_parser.Models;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Linq;

namespace CSV_dynamic_parser.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Fields Configuration.";

            return View();
        }

        // GET: Upload  

        [HttpGet]
        public ActionResult UploadFile()
        {
            List<Column> defaultColumns = new List<Column>();
            defaultColumns.Add(new Column { Id = 1, Name = "Title" });
            defaultColumns.Add(new Column { Id = 2, Name = "Description" });

            ViewBag.Headers = defaultColumns;

            //dropdown items from configuration

            List<string> itemsCopy = new List<string>();
            List<string> items = TempData["definedFields"] as List<string>;
            List<DropDownItem> dropDownItems = new List<DropDownItem>();

            if (items != null)
            {
                itemsCopy = items;
                if (items.Count > 0)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        dropDownItems.Add(new DropDownItem { Id = i, Name = items[i].ToString().Trim() });
                    }

                    ViewBag.DropDownItems = new SelectList(dropDownItems, "Id", "Name");
                }
                TempData["definedFields"] = itemsCopy;
            }
            else
            {
                List<DropDownItem> defaultDropDownItems = new List<DropDownItem>();
                defaultDropDownItems.Add(new DropDownItem { Id = 1, Name = "Title" });
                defaultDropDownItems.Add(new DropDownItem { Id = 2, Name = "Description" });

                ViewBag.DropDownItems = new SelectList(defaultDropDownItems, "Id", "Name");
            }

            return View();
        }

        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {
            try
            {
                List<ExcelViewModel> excelData = new List<ExcelViewModel>();

                if (file.ContentLength > 0)
                {
                    string _FileName = Path.GetFileName(file.FileName);
                    string _path = Path.Combine(Server.MapPath("~/UploadedFiles"), _FileName);
                    file.SaveAs(_path);
                    excelData = ReadExcel(_path);
                    TempData["excelData"] = excelData;
                }

                //dropdown items from configuration
                List<string> items = TempData["definedFields"] as List<string>;

                List<DropDownItem> dropDownItems = new List<DropDownItem>();

                if (items != null)
                {
                    if (items.Count > 0)
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            dropDownItems.Add(new DropDownItem { Id = i, Name = items[i].ToString().Trim() });
                        }

                        ViewBag.DropDownItems = new SelectList(dropDownItems, "Id", "Name");
                    }
                }
                else
                {
                    List<DropDownItem> defaultDropDownItems = new List<DropDownItem>();
                    defaultDropDownItems.Add(new DropDownItem { Id = 1, Name = "Title" });
                    defaultDropDownItems.Add(new DropDownItem { Id = 2, Name = "Description" });

                    ViewBag.DropDownItems = new SelectList(defaultDropDownItems, "Id", "Name");
                }

                // Input File headers
                List<string> headers = new List<string>();
                List<Column> columns = new List<Column>();
              
                if (excelData.Count > 0)
                {
                    headers = excelData.Find(x => x.excelColumns.Count > 0).excelColumns;
                    if (headers.Count > 0)
                    {
                        for (int i = 0; i < headers.Count; i++)
                        {
                            columns.Add(new Column { Id = i, Name = headers[i].ToString().Trim() });
                        }
                        ViewBag.Headers = columns;
                        ViewBag.RowCount = columns.Count;
                    }
                }

                
                ViewBag.Message = "File Uploaded Successfully!!";

                return View();
            }
            catch
            {
                ViewBag.Message = "File upload failed!!";
                return View();
            }
        }

        [HttpPost]
        public ActionResult Save(string list)
        {
            if (list != null)
            {
                List<string> definedFields = JsonConvert.DeserializeObject<List<string>>(list);
                if (definedFields.Count > 0)
                {
                    TempData["definedFields"] = definedFields;
                }
            }
            return RedirectToAction("UploadFile");
        }

        [HttpPost]
        public ActionResult SaveOne(string list)
        {
            if (list != null)
            {
                List<string> mappedFields = JsonConvert.DeserializeObject<List<string>>(list);
                if (mappedFields.Count > 0)
                {
                    TempData["mappedFields"] = mappedFields;
                }
            }
            return RedirectToAction("ResultPage");
        }

        public ActionResult ResultPage()
        {
            try
            {
                //retrieve mappedFields from tempData
                List<string> mappedFields = TempData["mappedFields"] as List<string>;
                //retrieve excel data from tempData
                List<ExcelViewModel> items = TempData["excelData"] as List<ExcelViewModel>;

                List<ExcelViewModel> result = new List<ExcelViewModel>();

                //mapping data
                if (mappedFields != null && items != null)
                {
                    var rowCount = 0;
                    if (mappedFields.Count > 0)
                    {
                        for (int index = 0; index < mappedFields.Count; index++)
                        {
                            if (mappedFields[index] != "not selected")
                            {
                                ExcelViewModel model = new ExcelViewModel();
                                model = items.Find(x => x.columnName == items[0].excelColumns.ElementAt(index).ToString());
                                model.columnName = mappedFields[index];
                                result.Add(model);
                                rowCount = model.columnValue.Count;
                            }
                        }
                    }
                    ViewBag.Columns = result;
                    ViewBag.RowCount = rowCount;
                }
                else
                {
                    result.Add(new ExcelViewModel
                    {
                        columnName = "",
                        columnValue = new List<string>() { "", "", "" }
                    });
                    ViewBag.Columns = result;
                }

                ViewBag.Message = "ResultPage display Successfully";

                return View();
            }
            catch
            {
                ViewBag.Message = "File upload failed!!";
                return View();
            }
        }

        public List<ExcelViewModel> ReadExcel(string FilePath)
        {
            try
            {
                List<string> excelColumns = new List<string>();
                List<ExcelViewModel> excelData = new List<ExcelViewModel>();

                FileInfo existingFile = new FileInfo(FilePath);
                using (ExcelPackage package = new ExcelPackage(existingFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];


                    //get the headers from the excel file
                    foreach (var firstRowCell in worksheet.Cells[worksheet.Dimension.Start.Row, worksheet.Dimension.Start.Column, 1, worksheet.Dimension.End.Column])
                        excelColumns.Add(firstRowCell.Text);

                    int columnCount = worksheet.Dimension.End.Column;
                    for (int column = 1; column <= columnCount; column++)
                    {
                        List<string> values = new List<string>();
                        foreach (var item in worksheet.Cells[2, column, worksheet.Dimension.End.Row, column])
                        {
                            values.Add(item.Text.ToString());
                        }
                        excelData.Add(new ExcelViewModel()
                        {
                            columnName = worksheet.Cells[1, column].Value.ToString().Trim(),
                            columnValue = values,
                            excelColumns = excelColumns
                        });
                    }
                }
                return excelData;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}