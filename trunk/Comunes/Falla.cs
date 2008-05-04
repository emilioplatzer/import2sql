/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 03/05/2008
 * Time: 11:37 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using NUnit.Framework;

namespace Comunes
{
	public class Falla
	{
		public static void Detener(string Mensaje){
			Assert.Fail(Mensaje);
		}
		public static void SiEsNulo(object objeto){
			Assert.IsNotNull(objeto);
		}
		public static void SiNoEsNulo(object objeto,string mensaje){
			Assert.IsNull(objeto,mensaje);
		}
	}
	public class Advertir
	{
		public static void SiEsFalso(bool condicion, string mensaje){
			Assert.IsTrue(condicion,mensaje);
		}
	}
}
