namespace JsonLinq.Controlls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JsonLinq.Services;

/// <summary>
/// Interaction logic for Linq.xaml
/// </summary>
public partial class Linq : UserControl
{
	private readonly JsonLinqService _service;
	public Linq()
	{
		_service = new JsonLinqService();

		InitializeComponent();
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		var json = Input.Text.Trim();
		var query = Query.Text.Trim();

		var result = _service.Process(json, query);
		Result.Text = result.Json;
		Count.Text = $"Count: {result.Count}";
	}
}
