using Hyperledger.Aries.Features.IssueCredential;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using static IDWallet.Models.DDL_Vehicle_License_Class;

namespace IDWallet.Models
{
    public class DDL : WalletElement
    {
        public StackLayout PersonalDataStack { get; set; }

        private Grid _permitGrid;
        public Grid PermitGrid
        {
            get => _permitGrid;
            set => SetProperty(ref _permitGrid, value);
        }

        private Thickness _eagleFrameMargin;
        public Thickness EagleFrameMargin
        {
            get => _eagleFrameMargin;
            set => SetProperty(ref _eagleFrameMargin, value);
        }
        private double _content_HeightRequest;
        public double Content_HeightRequest
        {
            get => _content_HeightRequest;
            set => SetProperty(ref _content_HeightRequest, value);
        }

        private double _scaledFontSize;
        public double ScaledFontSize
        {
            get => _scaledFontSize;
            set => SetProperty(ref _scaledFontSize, value);
        }

        private double _infoFontSize;
        public double InfoFontSize
        {
            get => _infoFontSize;
            set => SetProperty(ref _infoFontSize, value);
        }

        private bool _infoIsOpen;
        public bool InfoIsOpen
        {
            get => _infoIsOpen;
            set => SetProperty(ref _infoIsOpen, value);
        }

        public string DateOfIssuance { get; set; }
        public string IssuingEntity { get; set; }
        public string Id { get; set; }
        private readonly string _generalRestrictions;
        private List<DDL_Vehicle_License_Class> _licenseClasses;
        private bool _eagleFrameHeightCalculated = false;
        private double _permitGridHeightUsed = 0d;

        public DDL(CredentialRecord credentialRecord)
        {
            _licenseClasses = new List<DDL_Vehicle_License_Class>();
            PersonalDataStack = new StackLayout
            {
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.CenterAndExpand,
                Spacing = 0,
            };
            EagleFrameMargin = new Thickness(0, 5);
            Content_HeightRequest = 98;
            InfoIsOpen = false;

            string placeOfBirth = "";
            string dateOfBirth = "";
            ScaledFontSize = 8;
            InfoFontSize = 7;

            List<string> firstNames = new List<string>();
            List<string> familyNames = new List<string>();
            List<string> academicTitles = new List<string>();

            foreach (CredentialPreviewAttribute credentialPreviewAttribute in credentialRecord.CredentialAttributesValues)
            {
                if (credentialPreviewAttribute.Name.Contains("licenseCategory")
                    && credentialPreviewAttribute.Name.Contains("DateOfIssuance")
                    && credentialPreviewAttribute.Value != null
                    && credentialPreviewAttribute.Value.ToString() != "")
                {
                    try
                    {
                        string category = credentialPreviewAttribute.Name.Split('_')[0] + "_";
                        List<CredentialPreviewAttribute> categoryAttributes = (from CredentialPreviewAttribute attribute in credentialRecord.CredentialAttributesValues
                                                                               where attribute.Name.Contains(category)
                                                                               select attribute).ToList();
                        _licenseClasses.Add(new DDL_Vehicle_License_Class(categoryAttributes));
                    }
                    catch
                    {
                        //...
                    }
                }
                else if (!credentialPreviewAttribute.Name.Contains("licenseCategory"))
                {
                    switch (credentialPreviewAttribute.Name)
                    {
                        case "firstName":
                            firstNames = credentialPreviewAttribute.Value.ToString().Split(" ").ToList();
                            break;
                        case "familyName":
                            familyNames = credentialPreviewAttribute.Value.ToString().Split(" ").ToList();
                            break;
                        case "academicTitle":
                            academicTitles = credentialPreviewAttribute.Value.ToString().Split(" ").ToList();
                            break;
                        case "placeOfBirth":
                            placeOfBirth = credentialPreviewAttribute.Value.ToString();
                            break;
                        case "dateOfBirth":
                            try
                            {
                                dateOfBirth = DateTime.ParseExact(credentialPreviewAttribute.Value.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToShortDateString();
                            }
                            catch
                            {
                                try
                                {
                                    dateOfBirth = DateTime.ParseExact(credentialPreviewAttribute.Value.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture).ToShortDateString();
                                }
                                catch
                                {
                                    try
                                    {
                                        dateOfBirth = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((double)credentialPreviewAttribute.Value).ToShortDateString();
                                    }
                                    catch
                                    {
                                        dateOfBirth = "";
                                    }
                                }
                            }
                            break;
                        case "dateOfIssuance":
                            try
                            {
                                DateOfIssuance = DateTime.ParseExact(credentialPreviewAttribute.Value.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToShortDateString();
                            }
                            catch
                            {
                                try
                                {
                                    DateOfIssuance = DateTime.ParseExact(credentialPreviewAttribute.Value.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture).ToShortDateString();
                                }
                                catch
                                {
                                    try
                                    {
                                        DateOfIssuance = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((double)credentialPreviewAttribute.Value).ToShortDateString();
                                    }
                                    catch
                                    {
                                        DateOfIssuance = "";
                                    }
                                }
                            }
                            break;
                        case "issuingEntity":
                            IssuingEntity = credentialPreviewAttribute.Value.ToString();
                            break;
                        case "id":
                            Id = credentialPreviewAttribute.Value.ToString();
                            break;
                        case "generalRestrictions":
                            _generalRestrictions = credentialPreviewAttribute.Value.ToString();
                            break;
                        default:
                            break;
                    }
                }
            }

            academicTitles.AddRange(familyNames);
            firstNames.RemoveAll(x => string.IsNullOrEmpty(x));
            academicTitles.RemoveAll(x => string.IsNullOrEmpty(x));

            StackLayout familyNameStack = CreateFamilyNameStack(academicTitles);
            StackLayout firstNameStack = CreateFirstNameStack(firstNames);
            StackLayout birthDateAndPlaceStack = CreatePersonalDataStack(placeOfBirth, dateOfBirth);

            PersonalDataStack.Children.Add(familyNameStack);
            PersonalDataStack.Children.Add(firstNameStack);
            PersonalDataStack.Children.Add(birthDateAndPlaceStack);
        }

        private StackLayout CreateFirstNameStack(List<string> firstNames)
        {
            StackLayout firstNameStack = new StackLayout { Spacing = -2, HorizontalOptions = LayoutOptions.Start };

            Label currentLabel = new Label();
            FormattedString currentLabelText = new FormattedString();
            currentLabelText.Spans.Add(new Span { Text = "2. ", TextColor = (Color)Application.Current.Resources["DDLBlue"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold });
            Span currentSpan = new Span { Text = "", TextColor = (Color)Application.Current.Resources["DDLBlack"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold };
            while (firstNames.Any())
            {
                if (currentSpan.Text.Length + firstNames[0].Length < 26 || currentSpan.Text.Length == 0)
                {
                    currentSpan.Text += firstNames[0];
                    currentSpan.Text += " ";
                    firstNames.RemoveAt(0);
                }
                else
                {
                    currentLabelText.Spans.Add(currentSpan);
                    currentLabel.FormattedText = currentLabelText;
                    firstNameStack.Children.Add(currentLabel);
                    currentLabel = new Label();
                    currentLabelText = new FormattedString();
                    currentLabelText.Spans.Add(new Span { Text = "2. ", TextColor = Color.Transparent, FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold });
                    currentSpan = new Span { Text = "", TextColor = (Color)Application.Current.Resources["DDLBlack"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold };
                }
            }
            currentLabelText.Spans.Add(currentSpan);
            if (currentSpan.Text.Length < 26)
            {
                Span placeholderSpan = new Span { Text = "", TextColor = Color.Transparent, FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold };

                while (currentSpan.Text.Length + placeholderSpan.Text.Length < 26)
                {
                    placeholderSpan.Text += "L";
                }

                currentLabelText.Spans.Add(placeholderSpan);
            }
            currentLabel.FormattedText = currentLabelText;
            firstNameStack.Children.Add(currentLabel);

            return firstNameStack;
        }

        private StackLayout CreateFamilyNameStack(List<string> familyNames)
        {
            StackLayout familyNameStack = new StackLayout { Spacing = -2, HorizontalOptions = LayoutOptions.Start };
            Label currentLabel2 = new Label();
            FormattedString currentLabelText2 = new FormattedString();
            currentLabelText2.Spans.Add(new Span { Text = "1. ", TextColor = (Color)Application.Current.Resources["DDLBlue"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold });
            Span currentSpan2 = new Span { Text = "", TextColor = (Color)Application.Current.Resources["DDLBlack"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold };
            while (familyNames.Any())
            {
                if (currentSpan2.Text.Length + familyNames[0].Length < 26 || currentSpan2.Text.Length == 0)
                {
                    currentSpan2.Text += familyNames[0];
                    currentSpan2.Text += " ";
                    familyNames.RemoveAt(0);
                }
                else
                {
                    currentLabelText2.Spans.Add(currentSpan2);
                    currentLabel2.FormattedText = currentLabelText2;
                    familyNameStack.Children.Add(currentLabel2);
                    currentLabel2 = new Label();
                    currentLabelText2 = new FormattedString();
                    currentLabelText2.Spans.Add(new Span { Text = "1. ", TextColor = Color.Transparent, FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold });
                    currentSpan2 = new Span { Text = "", TextColor = (Color)Application.Current.Resources["DDLBlack"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold };
                }
            }
            currentLabelText2.Spans.Add(currentSpan2);
            currentLabel2.FormattedText = currentLabelText2;
            familyNameStack.Children.Add(currentLabel2);

            return familyNameStack;
        }

        private StackLayout CreatePersonalDataStack(string placeOfBirth, string dateOfBirth)
        {
            StackLayout birthDateAndPlaceStack = new StackLayout { Spacing = -2, HorizontalOptions = LayoutOptions.Start };
            List<string> placesOfBirth = placeOfBirth.Split(" ").ToList();
            Label currentLabel3 = new Label();
            FormattedString currentLabelText3 = new FormattedString();
            currentLabelText3.Spans.Add(new Span { Text = "3. ", TextColor = (Color)Application.Current.Resources["DDLBlue"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold });
            currentLabelText3.Spans.Add(new Span { Text = dateOfBirth + " ", TextColor = (Color)Application.Current.Resources["DDLBlack"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold });
            Span currentSpan3 = new Span { Text = "", TextColor = (Color)Application.Current.Resources["DDLBlack"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold };
            while (placesOfBirth.Any())
            {
                if (currentSpan3.Text.Length + placesOfBirth[0].Length < 16)
                {
                    currentSpan3.Text += placesOfBirth[0];
                    currentSpan3.Text += " ";
                    placesOfBirth.RemoveAt(0);
                }
                else if (currentSpan3.Text.Length < 10)
                {
                    string frontSub = placesOfBirth[0].Substring(0, 16 - currentSpan3.Text.Length);
                    string backSub = placesOfBirth[0].Substring(16);

                    currentSpan3.Text += frontSub;
                    currentSpan3.Text += "-";
                    placesOfBirth[0] = backSub;

                    currentLabelText3.Spans.Add(currentSpan3);
                    currentLabel3.FormattedText = currentLabelText3;
                    birthDateAndPlaceStack.Children.Add(currentLabel3);
                    currentLabel3 = new Label();
                    currentLabelText3 = new FormattedString();
                    currentLabelText3.Spans.Add(new Span { Text = "3. " + dateOfBirth + " ", TextColor = Color.Transparent, FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold });
                    currentSpan3 = new Span { Text = "", TextColor = (Color)Application.Current.Resources["DDLBlack"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold };
                }
                else
                {
                    currentLabelText3.Spans.Add(currentSpan3);
                    currentLabel3.FormattedText = currentLabelText3;
                    birthDateAndPlaceStack.Children.Add(currentLabel3);
                    currentLabel3 = new Label();
                    currentLabelText3 = new FormattedString();
                    currentLabelText3.Spans.Add(new Span { Text = "3. " + dateOfBirth + " ", TextColor = Color.Transparent, FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold });
                    currentSpan3 = new Span { Text = "", TextColor = (Color)Application.Current.Resources["DDLBlack"], FontSize = ScaledFontSize, FontAttributes = FontAttributes.Bold };
                }
            }
            currentLabelText3.Spans.Add(currentSpan3);
            currentLabel3.FormattedText = currentLabelText3;
            birthDateAndPlaceStack.Children.Add(currentLabel3);
            birthDateAndPlaceStack.PropertyChanged += BirthDataStack_PropertyChanged;

            return birthDateAndPlaceStack;
        }

        private async void BirthDataStack_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Height" && PersonalDataStack.Scale == 1)
            {
                StackLayout parentParentStack = PersonalDataStack.Parent.Parent as StackLayout;
                Frame eagleFrame = parentParentStack.Children[1] as Frame;

                PersonalDataStack.Scale = (parentParentStack.Width - eagleFrame.WidthRequest - parentParentStack.Spacing) / PersonalDataStack.Width;
                if (PersonalDataStack.Height * PersonalDataStack.Scale > eagleFrame.HeightRequest + eagleFrame.Margin.VerticalThickness)
                {
                    EagleFrameMargin = new Thickness(0, (PersonalDataStack.Height * PersonalDataStack.Scale - eagleFrame.HeightRequest) / 2 + 5);
                }
                ScaledFontSize = ((PersonalDataStack.Children[0] as StackLayout).Children[0] as Label).FormattedText.Spans[0].FontSize * PersonalDataStack.Scale;
                InfoFontSize = ScaledFontSize - 1;
                await CreatePermitTable();

                if (!_eagleFrameHeightCalculated)
                {
                    _eagleFrameHeightCalculated = true;
                    Content_HeightRequest += eagleFrame.HeightRequest + EagleFrameMargin.VerticalThickness;
                }
            }
        }

        private async Task CreatePermitTable()
        {
            Grid tableGrid = await CreateTableGrid();

            await CreateTableHeader(tableGrid);

            await SortLicenseClasses();

            for (int i = 0; i < _licenseClasses.Count; i++)
            {
                switch (_licenseClasses[i].ClassType)
                {
                    case DDL_Class_Type.AM:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_AM.svg",
                            ScaledFontSize * 6.89 / 4.32, ScaledFontSize, (Color)Application.Current.Resources["DDLBlue"]);
                        break;
                    case DDL_Class_Type.A1:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_A1.svg",
                            ScaledFontSize * 8.21 / 5.59, ScaledFontSize);
                        break;
                    case DDL_Class_Type.A2:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_A2.svg",
                            ScaledFontSize * 8.16 / 5.51, ScaledFontSize);
                        break;
                    case DDL_Class_Type.A:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_A.svg",
                            ScaledFontSize * 8.2 / 5.49, ScaledFontSize);
                        break;
                    case DDL_Class_Type.B1:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_B1.svg",
                            ScaledFontSize * 9.07 / 4.42, ScaledFontSize);
                        break;
                    case DDL_Class_Type.B:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_B.svg",
                            ScaledFontSize * 9.06 / 4.33, ScaledFontSize);
                        break;
                    case DDL_Class_Type.C1:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_C1.svg",
                            ScaledFontSize * 9.44 / 5.05, ScaledFontSize);
                        break;
                    case DDL_Class_Type.C:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_C.svg",
                            ScaledFontSize * 11.54 / 4.74, ScaledFontSize);
                        break;
                    case DDL_Class_Type.D1:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_D1.svg",
                            ScaledFontSize * 10.11 / 5.46, ScaledFontSize);
                        break;
                    case DDL_Class_Type.D:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_D.svg",
                            ScaledFontSize * 14.78 / 5.23, ScaledFontSize);
                        break;
                    case DDL_Class_Type.BE:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_BE.svg",
                            ScaledFontSize * 17.99 / 5.18, ScaledFontSize);
                        break;
                    case DDL_Class_Type.C1E:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_C1E.svg",
                            ScaledFontSize * 15.5 / 4.8, ScaledFontSize);
                        break;
                    case DDL_Class_Type.CE:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_CE.svg",
                            ScaledFontSize * 16.16 / 4.79, ScaledFontSize);
                        break;
                    case DDL_Class_Type.D1E:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_D1E.svg",
                            ScaledFontSize * 16.71 / 5.44, ScaledFontSize);
                        break;
                    case DDL_Class_Type.DE:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_DE.svg",
                            ScaledFontSize * 21.51 / 5.25, ScaledFontSize);
                        break;
                    case DDL_Class_Type.L:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_L.svg",
                            ScaledFontSize * 6.56 / 5.66, ScaledFontSize);
                        break;
                    case DDL_Class_Type.T:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_T.svg",
                            ScaledFontSize * 9.01 / 6.11, ScaledFontSize);
                        break;
                    case DDL_Class_Type.M:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_AM.svg",
                            ScaledFontSize * 6.89 / 4.32, ScaledFontSize);
                        break;
                    default:
                        _licenseClasses[i].IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_AM.svg",
                            ScaledFontSize * 6.89 / 4.32, ScaledFontSize);
                        break;
                }
                if (i == _licenseClasses.Count - 1)
                {
                    await CreateLastRow(tableGrid, _licenseClasses[i]);
                }
                else
                {
                    await CreateRegularRow(tableGrid, _licenseClasses[i]);
                }
            }

            if (!string.IsNullOrEmpty(_generalRestrictions))
            {
                await CreateGeneralRestrictionRow(tableGrid, _generalRestrictions);
            }

            tableGrid.PropertyChanged += TableGrid_PropertyChanged;
            PermitGrid = tableGrid;
        }

        private async Task SortLicenseClasses()
        {
            _licenseClasses = _licenseClasses.OrderBy(x => x.ClassType).ToList();
        }

        private void TableGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Height" && _permitGridHeightUsed != PermitGrid.Height)
            {
                Content_HeightRequest += PermitGrid.Height - _permitGridHeightUsed;
                _permitGridHeightUsed = PermitGrid.Height;
            }
        }

        private async Task<Grid> CreateTableGrid()
        {
            return new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                },
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height  = ScaledFontSize * 1.35 + .5 }
                },
                ColumnSpacing = 0,
                RowSpacing = 0
            };
        }

        private async Task CreateTableHeader(Grid tableGrid)
        {
            tableGrid.Children.Add(new Label
            {
                Text = "9.",
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = (Color)Application.Current.Resources["DDLBlue"],
                FontSize = ScaledFontSize,
            }, 0, 0);
            tableGrid.Children.Add(new Label
            {
                Text = "WWW",
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = Color.Transparent,
                FontSize = ScaledFontSize,
            }, 0, 0);
            tableGrid.Children.Add(new Label
            {
                Text = "10.",
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = (Color)Application.Current.Resources["DDLBlue"],
                FontSize = ScaledFontSize,
            }, 2, 0);
            tableGrid.Children.Add(new Label
            {
                Text = "11.",
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = (Color)Application.Current.Resources["DDLBlue"],
                FontSize = ScaledFontSize,
            }, 3, 0);
            tableGrid.Children.Add(new BoxView
            {
                HeightRequest = 0.5,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.End,
                Color = (Color)Application.Current.Resources["DDLBlue"],
            }, 0, 4, 0, 1);
        }

        private async Task CreateRegularRow(Grid tableGrid, DDL_Vehicle_License_Class license_Class)
        {
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = ScaledFontSize * 1.35 + .5 });
            int currentRow = tableGrid.RowDefinitions.Count - 1;

            tableGrid.Children.Add(new Label
            {
                Text = license_Class.Identifier,
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = (Color)Application.Current.Resources["DDLBlack"],
                FontSize = ScaledFontSize,
            }, 0, currentRow);
            tableGrid.Children.Add(new Frame
            {
                HeightRequest = ScaledFontSize,
                CornerRadius = 0,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Content = new Image { Source = license_Class.IconSource, VerticalOptions = LayoutOptions.FillAndExpand, HorizontalOptions = LayoutOptions.StartAndExpand },
                Margin = new Thickness(0, 2, 15, 3)
            }, 1, currentRow);
            tableGrid.Children.Add(new Label
            {
                Text = license_Class.IssuingDate,
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = (Color)Application.Current.Resources["DDLBlack"],
                FontSize = ScaledFontSize,
            }, 2, currentRow);
            if (license_Class.ExpiryDate == "-")
            {
                tableGrid.Children.Add(new Label
                {
                    Text = " " + license_Class.ExpiryDate,
                    Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                    TextColor = (Color)Application.Current.Resources["DDLBlack"],
                    FontSize = ScaledFontSize,
                    ScaleX = 2
                }, 3, currentRow);
            }
            else
            {
                tableGrid.Children.Add(new Label
                {
                    Text = license_Class.ExpiryDate,
                    Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                    TextColor = (Color)Application.Current.Resources["DDLBlack"],
                    FontSize = ScaledFontSize,
                }, 3, currentRow);
            }

            if (!string.IsNullOrEmpty(license_Class.Restrictions))
            {
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                currentRow = tableGrid.RowDefinitions.Count - 1;

                tableGrid.Children.Add(new Label
                {
                    Text = "12.",
                    Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                    TextColor = (Color)Application.Current.Resources["DDLBlue"],
                    VerticalOptions = LayoutOptions.Start,
                    FontSize = ScaledFontSize,
                }, 1, currentRow);

                tableGrid.Children.Add(new Label
                {
                    Text = license_Class.Restrictions,
                    Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                    TextColor = (Color)Application.Current.Resources["DDLBlack"],
                    FontSize = ScaledFontSize,
                    LineBreakMode = LineBreakMode.CharacterWrap,
                    MaxLines = 10
                }, 2, 4, currentRow, currentRow + 1);
            }

            tableGrid.Children.Add(new BoxView
            {
                HeightRequest = 0.5,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.End,
                Color = (Color)Application.Current.Resources["DDLBlue"],
            }, 0, 4, currentRow, currentRow + 1);
        }

        private async Task CreateLastRow(Grid tableGrid, DDL_Vehicle_License_Class license_Class)
        {
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = (ScaledFontSize * 1.35 + .5) * 1.2 });
            int currentRow = tableGrid.RowDefinitions.Count - 1;

            tableGrid.Children.Add(new Label
            {
                Text = license_Class.Identifier,
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = (Color)Application.Current.Resources["DDLBlack"],
                FontSize = ScaledFontSize,
            }, 0, currentRow);
            tableGrid.Children.Add(new Frame
            {
                HeightRequest = ScaledFontSize,
                CornerRadius = 0,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Content = new Image { Source = license_Class.IconSource, VerticalOptions = LayoutOptions.FillAndExpand, HorizontalOptions = LayoutOptions.StartAndExpand },
                Margin = new Thickness(0, 2, 15, 3)
            }, 1, currentRow);
            tableGrid.Children.Add(new Label
            {
                Text = license_Class.IssuingDate,
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = (Color)Application.Current.Resources["DDLBlack"],
                FontSize = ScaledFontSize,
            }, 2, currentRow);
            if (license_Class.ExpiryDate == "-")
            {
                tableGrid.Children.Add(new Label
                {
                    Text = " " + license_Class.ExpiryDate,
                    Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                    TextColor = (Color)Application.Current.Resources["DDLBlack"],
                    FontSize = ScaledFontSize,
                    ScaleX = 2
                }, 3, currentRow);
            }
            else
            {
                tableGrid.Children.Add(new Label
                {
                    Text = license_Class.ExpiryDate,
                    Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                    TextColor = (Color)Application.Current.Resources["DDLBlack"],
                    FontSize = ScaledFontSize,
                }, 3, currentRow);
            }

            if (!string.IsNullOrEmpty(license_Class.Restrictions))
            {
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                currentRow = tableGrid.RowDefinitions.Count - 1;

                tableGrid.Children.Add(new Label
                {
                    Text = "12.",
                    Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                    TextColor = (Color)Application.Current.Resources["DDLBlue"],
                    VerticalOptions = LayoutOptions.Start,
                    FontSize = ScaledFontSize,
                }, 1, currentRow);

                tableGrid.Children.Add(new Label
                {
                    Text = license_Class.Restrictions,
                    Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                    TextColor = (Color)Application.Current.Resources["DDLBlack"],
                    FontSize = ScaledFontSize,
                    LineBreakMode = LineBreakMode.CharacterWrap,
                    MaxLines = 10
                }, 2, 4, currentRow, currentRow + 1);
            }

            tableGrid.Children.Add(new BoxView
            {
                HeightRequest = 0.75,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.End,
                Color = (Color)Application.Current.Resources["DDLBlue"],
            }, 0, 4, currentRow, currentRow + 1);
        }

        private async Task CreateGeneralRestrictionRow(Grid tableGrid, string generalRestrictions)
        {
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = ScaledFontSize * 1.35 + .5 });
            int currentRow = tableGrid.RowDefinitions.Count - 1;

            tableGrid.Children.Add(new Label
            {
                Text = "12.",
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = (Color)Application.Current.Resources["DDLBlue"],
                FontSize = ScaledFontSize,
                Margin = new Thickness(0, 2, 0, 0)
            }, 0, currentRow);
            tableGrid.Children.Add(new Label
            {
                Text = "WWW",
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = Color.Transparent,
                FontSize = ScaledFontSize,
                Margin = new Thickness(0, 2, 0, 0)
            }, 0, currentRow);
            //tableGrid.Children.Add(new Frame
            //{
            //    HeightRequest = ScaledFontSize,
            //    CornerRadius = 0,
            //    HorizontalOptions = LayoutOptions.StartAndExpand,
            //    Content = new Image { Aspect = Aspect.AspectFit, Source = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.Info_Icon.svg", (Color)Application.Current.Resources["DDLBlue"]) },
            //    Margin = new Thickness(0, 3, 35, 0)
            //}, 1, currentRow);
            tableGrid.Children.Add(new Label
            {
                Text = generalRestrictions,
                Style = (Style)Application.Current.Resources["DDL_Table_Label"],
                TextColor = (Color)Application.Current.Resources["DDLBlack"],
                FontSize = ScaledFontSize,
                Margin = new Thickness(0, 2, 0, 0)
            }, 2, 4, currentRow, currentRow + 1);
        }
    }
}
